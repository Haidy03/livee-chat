using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Contacts;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/contacts")]
public sealed class ContactsController : ControllerBase
{
    private readonly IContactService _service;
    private readonly ICurrentUser _currentUser;

    public ContactsController(IContactService service, ICurrentUser currentUser) { _service = service; _currentUser = currentUser; }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] ContactSearchRequest request, CancellationToken ct)
    {
        var result = await _service.SearchContactsAsync(_currentUser.TenantId, request, ct);
        return Ok(ApiResponse<PagedResponse<ContactResponse>>.Ok(result.Value));
    }

    [HttpGet("{contactId}")]
    public async Task<IActionResult> Get(string contactId, CancellationToken ct)
    {
        var result = await _service.GetContactAsync(contactId, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse<ContactResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<ContactResponse>.Ok(result.Value));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContactRequest request, CancellationToken ct)
    {
        var result = await _service.CreateContactAsync(_currentUser.TenantId, _currentUser.UserId, request, ct);
        return CreatedAtAction(nameof(Get), new { contactId = result.Value.Id }, ApiResponse<ContactResponse>.Ok(result.Value));
    }

    [HttpPatch("{contactId}")]
    public async Task<IActionResult> Update(string contactId, [FromBody] UpdateContactRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateContactAsync(contactId, _currentUser.TenantId, request, ct);
        if (result.IsFailure) return NotFound(ApiResponse<ContactResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<ContactResponse>.Ok(result.Value));
    }

    [HttpDelete("{contactId}")]
    public async Task<IActionResult> Delete(string contactId, CancellationToken ct)
    {
        var result = await _service.DeleteContactAsync(contactId, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse.Fail(result.Error.Description));
        return NoContent();
    }
}
