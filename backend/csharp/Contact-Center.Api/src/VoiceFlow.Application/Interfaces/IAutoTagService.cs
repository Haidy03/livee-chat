using System.Text.Json;
using VoiceFlow.Contracts.AutoTags;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface IAutoTagService
{
    Task<Result<IEnumerable<AutoTagResponse>>> GetAutoTagsAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<Result<AutoTagResponse>> CreateAutoTagAsync(string tenantId, string userId, CreateAutoTagRequest request, CancellationToken cancellationToken = default);
    Task<Result<AutoTagResponse>> UpdateAutoTagAsync(string id, string tenantId, JsonElement patch, CancellationToken cancellationToken = default);
    Task<Result> DeleteAutoTagAsync(string id, string tenantId, CancellationToken cancellationToken = default);
}
