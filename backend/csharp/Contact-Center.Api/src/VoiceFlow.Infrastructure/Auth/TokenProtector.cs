using Microsoft.AspNetCore.DataProtection;
using VoiceFlow.Core.Interfaces.Services;

namespace VoiceFlow.Infrastructure.Auth;



public sealed class DataProtectionTokenProtector : ITokenProtector
{
    private readonly IDataProtector _access;
    private readonly IDataProtector _refresh;

    public DataProtectionTokenProtector(IDataProtectionProvider provider)
    {
        _access = provider.CreateProtector("HubSpot.AccessToken");
        _refresh = provider.CreateProtector("HubSpot.RefreshToken");
    }

    public string ProtectAccessToken(string plaintext) => _access.Protect(plaintext);
    public string UnprotectAccessToken(string ciphertext) => _access.Unprotect(ciphertext);
    public string ProtectRefreshToken(string plaintext) => _refresh.Protect(plaintext);
    public string UnprotectRefreshToken(string ciphertext) => _refresh.Unprotect(ciphertext);
}
