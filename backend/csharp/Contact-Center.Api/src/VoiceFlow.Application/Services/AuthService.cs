using MongoDB.Bson;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Auth;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Core.Interfaces.Services;

namespace VoiceFlow.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IAuthUserRepository _authUserRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IEditLogService _editLogService;
    private const int RefreshTokenExpiryDays = 30;
    private const int AccessTokenExpirySeconds = 3600;

    public AuthService(
        IAuthUserRepository authUserRepository,
        IAccountRepository accountRepository,
        IProfileRepository profileRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        IRefreshTokenStore refreshTokenStore,
        IEditLogService editLogService)
    {
        _authUserRepository = authUserRepository;
        _accountRepository = accountRepository;
        _profileRepository = profileRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _refreshTokenStore = refreshTokenStore;
        _editLogService = editLogService;
    }

    public async Task<Result<TokenResponse>> SignupAsync(SignupRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _authUserRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            return Result.Failure<TokenResponse>(Error.Conflict("AuthUser", "An account with this email already exists."));

        var authUser = new AuthUser
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsEmailConfirmed = false
        };
        await _authUserRepository.InsertAsync(authUser, cancellationToken);

        var account = new Account
        {
            UserId = authUser.Id,
            OrgName = request.OrgName
        };
        await _accountRepository.InsertAsync(account, cancellationToken);

        var profile = new Profile
        {
            UserId = authUser.Id,
            TenantId = account.Id,
            Email = authUser.Email,
            DisplayName = request.DisplayName ?? request.Email.Split('@')[0],
            Status = "online",
            Role = "owner"
        };
        await _profileRepository.InsertAsync(profile, cancellationToken);

        await _emailService.SendWelcomeEmailAsync(authUser.Email, profile.DisplayName, cancellationToken);

        return await IssueTokensAsync(authUser, account.Id, JwtRolesFromProfile(profile), cancellationToken);
    }

    public async Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        const string genericError = "Invalid email, organization, or password.";

        var authUser = await _authUserRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (authUser is null || !_passwordHasher.Verify(request.Password, authUser.PasswordHash))
            return Result.Failure<TokenResponse>(Error.Unauthorized(genericError));

        var profile = await _profileRepository.GetByUserIdAsync(authUser.Id, cancellationToken);
        if (profile is null)
            return Result.Failure<TokenResponse>(Error.Unauthorized(genericError));

        var account = await _accountRepository.GetByIdAsync(profile.TenantId, cancellationToken);
        if (account is null ||
            !string.Equals(
                (account.OrgName ?? string.Empty).Trim(),
                (request.OrgName ?? string.Empty).Trim(),
                StringComparison.OrdinalIgnoreCase))
            return Result.Failure<TokenResponse>(Error.Unauthorized(genericError));

        authUser.LastLoginAt = DateTime.UtcNow;
        await _authUserRepository.UpdateAsync(authUser, cancellationToken);

        await _editLogService.LogAsync(
            profile.TenantId,
            authUser.Id,
            "Auth",
            authUser.Id,
            "login",
            summary: $"User {authUser.Email} logged in.",
            cancellationToken: cancellationToken);

        return await IssueTokensAsync(authUser, profile.TenantId, JwtRolesFromProfile(profile), cancellationToken);
    }

    public async Task<Result<TokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var (userId, tenantId, isValid) = await _refreshTokenStore.ValidateAsync(refreshToken, cancellationToken);
        if (!isValid)
            return Result.Failure<TokenResponse>(Error.Unauthorized("Invalid or expired refresh token."));

        await _refreshTokenStore.RevokeAsync(refreshToken, cancellationToken);

        var authUser = await _authUserRepository.GetByIdAsync(userId, cancellationToken);
        if (authUser is null)
            return Result.Failure<TokenResponse>(Error.NotFound("AuthUser", userId));

        var profile = await _profileRepository.GetByUserIdAndTenantAsync(userId, tenantId, cancellationToken);
        if (profile is null)
            return Result.Failure<TokenResponse>(Error.NotFound("Profile", userId));

        return await IssueTokensAsync(authUser, tenantId, JwtRolesFromProfile(profile), cancellationToken);
    }

    public async Task<Result> LogoutAsync(string userId, string tenantId, CancellationToken cancellationToken = default)
    {
        await _refreshTokenStore.RevokeAllForUserAsync(userId, tenantId, cancellationToken);
        await _editLogService.LogAsync(
            tenantId,
            userId,
            "Auth",
            userId,
            "logout",
            summary: "User logged out.",
            cancellationToken: cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RecoverPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        var authUser = await _authUserRepository.GetByEmailAsync(email, cancellationToken);
        if (authUser is null)
            return Result.Success();

        authUser.PasswordResetToken = Guid.NewGuid().ToString("N");
        authUser.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(2);
        await _authUserRepository.UpdateAsync(authUser, cancellationToken);

        var resetLink = $"/auth/reset-password?token={authUser.PasswordResetToken}";
        await _emailService.SendPasswordResetEmailAsync(authUser.Email, resetLink, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var authUser = await _authUserRepository.GetByResetTokenAsync(request.Token, cancellationToken);
        if (authUser is null || authUser.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            return Result.Failure(Error.Unauthorized("Invalid or expired reset token."));

        authUser.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        authUser.PasswordResetToken = null;
        authUser.PasswordResetTokenExpiresAt = null;
        await _authUserRepository.UpdateAsync(authUser, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<UserResponse>> GetCurrentUserAsync(string userId, string tenantId, CancellationToken cancellationToken = default)
    {
        var authUser = await _authUserRepository.GetByIdAsync(userId, cancellationToken);
        if (authUser is null)
            return Result.Failure<UserResponse>(Error.NotFound("AuthUser", userId));

        var profile = await _profileRepository.GetByUserIdAndTenantAsync(userId, tenantId, cancellationToken);

        return new UserResponse
        {
            UserId = authUser.Id,
            Email = authUser.Email,
            TenantId = tenantId,
            DisplayName = profile?.DisplayName ?? authUser.Email,
            Roles = JwtRolesFromProfile(profile).ToList()
        };
    }

    public async Task<Result<string>> InviteUserAsync(string tenantId, InviteUserRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _authUserRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            return Result.Failure<string>(Error.Conflict("AuthUser", "An account with this email already exists."));

        var tenantProfiles = await _profileRepository.GetByTenantAsync(tenantId, cancellationToken);
        if (!tenantProfiles.Any())
            return Result.Failure<string>(Error.NotFound("Tenant", tenantId));

        var authUser = new AuthUser
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsEmailConfirmed = false
        };
        await _authUserRepository.InsertAsync(authUser, cancellationToken);

        var role = request.Role.Trim().ToLowerInvariant() switch
        {
            "owner" => "owner",
            "admin" => "admin",
            _ => "agent"
        };

        var display = (request.DisplayName ?? $"{request.FirstName} {request.LastName}".Trim()).Trim();
        if (string.IsNullOrWhiteSpace(display))
            display = authUser.Email.Split('@')[0];

        var profile = new Profile
        {
            UserId = authUser.Id,
            TenantId = tenantId,
            Email = authUser.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DisplayName = display,
            Timezone = request.Timezone,
            Language = request.Language,
            BrowserNotifications = request.BrowserNotifications,
            Role = role,
            Groups = request.Groups ?? [],
            ExtensionNumber = request.ExtensionNumber,
            Status = "offline"
        };

        await _profileRepository.InsertAsync(profile, cancellationToken);
        return authUser.Id;
    }

    private static IEnumerable<string> JwtRolesFromProfile(Profile? profile)
    {
        var r = (profile?.Role ?? "agent").Trim().ToLowerInvariant();
        yield return r switch
        {
            "owner" => "Owner",
            "admin" => "admin",
            _ => "agent"
        };
    }

    private async Task<Result<TokenResponse>> IssueTokensAsync(
        AuthUser authUser, string tenantId, IEnumerable<string> roles,
        CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(authUser.Id, tenantId, authUser.Email, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

        await _refreshTokenStore.SaveAsync(authUser.Id, tenantId, refreshToken, expiresAt, cancellationToken);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = AccessTokenExpirySeconds
        };
    }
}
