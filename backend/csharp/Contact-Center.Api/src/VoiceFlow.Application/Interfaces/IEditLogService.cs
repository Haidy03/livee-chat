using System.Text.Json;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.EditLogs;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface IEditLogService
{
    Task<Result<PagedResponse<EditLogResponse>>> SearchAsync(string tenantId, EditLogSearchRequest request, CancellationToken cancellationToken = default);
    Task<Result<EditLogResponse>> CreateAsync(string tenantId, string userId, CreateEditLogRequest request, CancellationToken cancellationToken = default);
    Task LogAsync(string tenantId, string userId, string entityType, string entityId, string action, string? field = null, JsonElement? oldValue = null, JsonElement? newValue = null, string? summary = null, CancellationToken cancellationToken = default);
}
