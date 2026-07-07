using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
