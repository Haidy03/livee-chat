using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Contacts;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface IContactService
{
    Task<Result<PagedResponse<ContactResponse>>> SearchContactsAsync(string tenantId, ContactSearchRequest request, CancellationToken cancellationToken = default);
    Task<Result<ContactResponse>> GetContactAsync(string contactId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<ContactResponse>> CreateContactAsync(string tenantId, string userId, CreateContactRequest request, CancellationToken cancellationToken = default);
    Task<Result<ContactResponse>> UpdateContactAsync(string contactId, string tenantId, UpdateContactRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteContactAsync(string contactId, string tenantId, CancellationToken cancellationToken = default);
}
