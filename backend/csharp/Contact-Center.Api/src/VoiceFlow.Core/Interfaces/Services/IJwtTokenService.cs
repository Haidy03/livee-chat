using System.Security.Claims;

namespace VoiceFlow.Core.Interfaces.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(string userId, string tenantId, string email, IEnumerable<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}
