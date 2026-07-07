using Microsoft.AspNetCore.DataProtection;

namespace CtiBackend.Services.Security;

public interface ITokenProtector
{
    string ProtectAccessToken(string plaintext);
    string UnprotectAccessToken(string ciphertext);
    string ProtectRefreshToken(string plaintext);
    string UnprotectRefreshToken(string ciphertext);
}

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
