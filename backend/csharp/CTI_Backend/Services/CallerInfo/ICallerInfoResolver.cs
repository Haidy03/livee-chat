using CtiBackend.Models.Cti;

namespace CtiBackend.Services.CallerInfo;

public interface ICallerInfoResolver
{
    Task<CallerInfoModel?> ResolveAsync(string? tenantId, string? phoneNumber, CancellationToken ct);
    Task<CallerInfoModel?> ResolveFromDirectoryAsync(string? tenantId, string? phoneNumber, CancellationToken ct);
}
