using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Contacts;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Application.Services;

public sealed class ContactService : IContactService
{
    private readonly IContactRepository _contactRepository;

    public ContactService(IContactRepository contactRepository) => _contactRepository = contactRepository;

    public async Task<Result<PagedResponse<ContactResponse>>> SearchContactsAsync(string tenantId, ContactSearchRequest request, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _contactRepository.SearchAsync(tenantId, request.Query, request.TagIds, request.Skip, request.PageSize, cancellationToken);
        return PagedResponse<ContactResponse>.Create(items.Select(MapToResponse).ToList().AsReadOnly(), request.Page, request.PageSize, total);
    }

    public async Task<Result<ContactResponse>> GetContactAsync(string contactId, string tenantId, CancellationToken cancellationToken = default)
    {
        var contact = await _contactRepository.GetByIdAsync(contactId, cancellationToken);
        if (contact is null || contact.TenantId != tenantId)
            return Result.Failure<ContactResponse>(Error.NotFound("Contact", contactId));
        return MapToResponse(contact);
    }

    public async Task<Result<ContactResponse>> CreateContactAsync(string tenantId, string userId, CreateContactRequest request, CancellationToken cancellationToken = default)
    {
        var contact = new Contact { TenantId = tenantId, UserId = userId, Name = request.Name, Phone = request.Phone, Email = request.Email, Company = request.Company, TagIds = request.TagIds, Notes = request.Notes };
        await _contactRepository.InsertAsync(contact, cancellationToken);
        return MapToResponse(contact);
    }

    public async Task<Result<ContactResponse>> UpdateContactAsync(string contactId, string tenantId, UpdateContactRequest request, CancellationToken cancellationToken = default)
    {
        var contact = await _contactRepository.GetByIdAsync(contactId, cancellationToken);
        if (contact is null || contact.TenantId != tenantId)
            return Result.Failure<ContactResponse>(Error.NotFound("Contact", contactId));

        if (request.Name is not null) contact.Name = request.Name;
        if (request.Phone is not null) contact.Phone = request.Phone;
        if (request.Email is not null) contact.Email = request.Email;
        if (request.Company is not null) contact.Company = request.Company;
        if (request.TagIds is not null) contact.TagIds = request.TagIds;
        if (request.Notes is not null) contact.Notes = request.Notes;

        await _contactRepository.UpdateAsync(contact, cancellationToken);
        return MapToResponse(contact);
    }

    public async Task<Result> DeleteContactAsync(string contactId, string tenantId, CancellationToken cancellationToken = default)
    {
        var contact = await _contactRepository.GetByIdAsync(contactId, cancellationToken);
        if (contact is null || contact.TenantId != tenantId)
            return Result.Failure(Error.NotFound("Contact", contactId));
        await _contactRepository.DeleteAsync(contactId, cancellationToken);
        return Result.Success();
    }

    private static ContactResponse MapToResponse(Contact c) => new() { Id = c.Id, Name = c.Name, Phone = c.Phone, Email = c.Email, Company = c.Company, TagIds = c.TagIds, Notes = c.Notes, LastCallAt = c.LastCallAt, TotalCalls = c.TotalCalls };
}
