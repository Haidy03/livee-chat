using VoiceFlow.Contracts.Auth;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface IAuthService
{
    Task<Result<TokenResponse>> SignupAsync(SignupRequest request, CancellationToken cancellationToken = default);
    Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<TokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(string userId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result> RecoverPasswordAsync(string email, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserResponse>> GetCurrentUserAsync(string userId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<string>> InviteUserAsync(string tenantId, InviteUserRequest request, CancellationToken cancellationToken = default);
}
