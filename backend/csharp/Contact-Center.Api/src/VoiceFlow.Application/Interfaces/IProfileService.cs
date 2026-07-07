using VoiceFlow.Contracts.Profiles;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface IProfileService
{
    Task<Result<IEnumerable<ProfileResponse>>> ListProfilesForTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<Result<ProfileResponse>> GetProfileAsync(string userId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<ProfileResponse>> UpdateProfileAsync(string userId, string tenantId, PatchUserProfileRequest request, CancellationToken cancellationToken = default);
}
