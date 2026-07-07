using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Tags;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Interfaces.Repositories;
using TagEntity = VoiceFlow.Core.Entities.Tag;

namespace VoiceFlow.Application.Services;

public sealed class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;

    public TagService(ITagRepository tagRepository) => _tagRepository = tagRepository;

    public async Task<Result<IEnumerable<TagResponse>>> GetTagsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var tags = await _tagRepository.GetByTenantAsync(tenantId, cancellationToken);
        return Result.Success(tags.Select(MapToResponse));
    }

    public async Task<Result<TagResponse>> CreateTagAsync(string tenantId, string userId, CreateTagRequest request, CancellationToken cancellationToken = default)
    {
        var tag = new TagEntity { TenantId = tenantId, UserId = userId, Label = request.Label, Color = request.Color };
        await _tagRepository.InsertAsync(tag, cancellationToken);
        return MapToResponse(tag);
    }

    public async Task<Result<TagResponse>> UpdateTagAsync(string tagId, string tenantId, UpdateTagRequest request, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdAsync(tagId, cancellationToken);
        if (tag is null || tag.TenantId != tenantId)
            return Result.Failure<TagResponse>(Error.NotFound("Tag", tagId));

        if (request.Label is not null) tag.Label = request.Label;
        if (request.Color is not null) tag.Color = request.Color;

        await _tagRepository.UpdateAsync(tag, cancellationToken);
        return MapToResponse(tag);
    }

    public async Task<Result> DeleteTagAsync(string tagId, string tenantId, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdAsync(tagId, cancellationToken);
        if (tag is null || tag.TenantId != tenantId)
            return Result.Failure(Error.NotFound("Tag", tagId));
        await _tagRepository.DeleteAsync(tagId, cancellationToken);
        return Result.Success();
    }

    private static TagResponse MapToResponse(TagEntity t) => new() { Id = t.Id, Label = t.Label, Color = t.Color };
}
