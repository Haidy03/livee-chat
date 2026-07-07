using VoiceFlow.Contracts.Tags;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface ITagService
{
    Task<Result<IEnumerable<TagResponse>>> GetTagsAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<Result<TagResponse>> CreateTagAsync(string tenantId, string userId, CreateTagRequest request, CancellationToken cancellationToken = default);
    Task<Result<TagResponse>> UpdateTagAsync(string tagId, string tenantId, UpdateTagRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteTagAsync(string tagId, string tenantId, CancellationToken cancellationToken = default);
}
