using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IAuthUserRepository : IRepository<AuthUser>
{
    Task<AuthUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AuthUser?> GetByResetTokenAsync(string token, CancellationToken cancellationToken = default);
}
