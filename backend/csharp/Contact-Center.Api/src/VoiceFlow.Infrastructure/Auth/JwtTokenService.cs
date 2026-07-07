using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VoiceFlow.Core.Interfaces.Services;
using VoiceFlow.Infrastructure.Options;

namespace VoiceFlow.Infrastructure.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenService(IOptions<JwtSettings> options, IWebHostEnvironmentAccessor envAccessor)
    {
        _settings = options.Value;

        var privateKeyPath = Path.Combine(envAccessor.ContentRootPath, _settings.PrivateKeyPath);
        var publicKeyPath = Path.Combine(envAccessor.ContentRootPath, _settings.PublicKeyPath);

        var rsaPrivate = RSA.Create();
        rsaPrivate.ImportFromPem(File.ReadAllText(privateKeyPath));
        _signingCredentials = new SigningCredentials(
            new RsaSecurityKey(rsaPrivate),
            SecurityAlgorithms.RsaSha256);

        var rsaPublic = RSA.Create();
        rsaPublic.ImportFromPem(File.ReadAllText(publicKeyPath));
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsaPublic),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }

    public string GenerateAccessToken(string userId, string tenantId, string email, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", tenantId),
            //new("role" , roles?.FirstOrDefault()??"Agent")
        };

        claims.AddRange(roles.Select(r => new Claim("role", r)));                                                                                                                               

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes),
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, _validationParameters, out _);
        }
        catch
        {
            return null;
        }
    }
}

public interface IWebHostEnvironmentAccessor
{
    string ContentRootPath { get; }
}

public sealed class WebHostEnvironmentAccessor : IWebHostEnvironmentAccessor
{
    public WebHostEnvironmentAccessor(IHostEnvironment env) =>
        ContentRootPath = env.ContentRootPath;

    public string ContentRootPath { get; }
}
