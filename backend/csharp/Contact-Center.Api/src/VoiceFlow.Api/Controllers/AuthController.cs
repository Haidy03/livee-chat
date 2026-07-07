using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Auth;
using VoiceFlow.Contracts.Common;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUser _currentUser;

    public AuthController(IAuthService authService, ICurrentUser currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    [HttpPost("signup")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request, CancellationToken ct)
    {
        var result = await _authService.SignupAsync(request, ct);
        if (result.IsFailure)
            return Conflict(ApiResponse<TokenResponse>.Fail(result.Error.Description));

        return CreatedAtAction(nameof(Me), ApiResponse<TokenResponse>.Ok(result.Value));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        if (result.IsFailure)
            return Unauthorized(ApiResponse<TokenResponse>.Fail(result.Error.Description));

        return Ok(ApiResponse<TokenResponse>.Ok(result.Value));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ct);
        if (result.IsFailure)
            return Unauthorized(ApiResponse<TokenResponse>.Fail(result.Error.Description));

        return Ok(ApiResponse<TokenResponse>.Ok(result.Value));
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _authService.LogoutAsync(_currentUser.UserId, _currentUser.TenantId, ct);
        return Ok(ApiResponse.Ok("Logged out successfully."));
    }

    [Authorize]
    [HttpPost("invite")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Invite([FromBody] InviteUserRequest request, CancellationToken ct)
    {
        if (!_currentUser.Roles.Any(r => string.Equals(r, "owner", StringComparison.OrdinalIgnoreCase)
                                         || string.Equals(r, "admin", StringComparison.OrdinalIgnoreCase)))
            return Forbid();

        var result = await _authService.InviteUserAsync(_currentUser.TenantId, request, ct);
        if (result.IsFailure)
            return Conflict(ApiResponse.Fail(result.Error.Description));

        return Ok(ApiResponse<object>.Ok(new { userId = result.Value }));
    }

    [HttpPost("recover")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Recover([FromBody] PasswordRecoveryRequest request, CancellationToken ct)
    {
        await _authService.RecoverPasswordAsync(request.Email, ct);
        return Ok(ApiResponse.Ok("If this email exists, a password reset link has been sent."));
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _authService.ResetPasswordAsync(request, ct);
        if (result.IsFailure)
            return Unauthorized(ApiResponse.Fail(result.Error.Description));

        return Ok(ApiResponse.Ok("Password reset successfully."));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await _authService.GetCurrentUserAsync(_currentUser.UserId, _currentUser.TenantId, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse<UserResponse>.Fail(result.Error.Description));

        return Ok(ApiResponse<UserResponse>.Ok(result.Value));
    }
}
