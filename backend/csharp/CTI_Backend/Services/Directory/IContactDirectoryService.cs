using CTI.Models.Directory;

namespace CtiBackend.Services.Directory;

/// <summary>
/// Looks up contacts in the Light CRM Mongo `contacts` collection by phone
/// number. Returns null on miss or any infrastructure failure — never throws.
/// </summary>
public interface IContactDirectoryService
{
    Task<ContactDocument?> FindByPhoneAsync(string? tenantId, string phone, CancellationToken ct);
}
