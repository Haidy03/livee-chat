namespace VoiceFlow.Infrastructure.Options;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "voiceflow-api";
    public string Audience { get; set; } = "voiceflow-clients";
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 30;
    public string PrivateKeyPath { get; set; } = "keys/private.pem";
    public string PublicKeyPath { get; set; } = "keys/public.pem";
}
