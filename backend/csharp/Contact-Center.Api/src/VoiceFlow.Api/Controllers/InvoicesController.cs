using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Invoices;
using VoiceFlow.Core.Enums;

namespace VoiceFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/invoices")]
public sealed class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _service;
    private readonly ICurrentUser _currentUser;

    public InvoicesController(IInvoiceService service, ICurrentUser currentUser) { _service = service; _currentUser = currentUser; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest pagination, [FromQuery] InvoiceStatus? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var result = await _service.GetInvoicesAsync(_currentUser.TenantId, status, from, to, pagination, ct);
        return Ok(ApiResponse<PagedResponse<InvoiceResponse>>.Ok(result.Value));
    }

    [HttpGet("{invoiceId}")]
    public async Task<IActionResult> Get(string invoiceId, CancellationToken ct)
    {
        var result = await _service.GetInvoiceAsync(invoiceId, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse<InvoiceResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<InvoiceResponse>.Ok(result.Value));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request, CancellationToken ct)
    {
        var result = await _service.CreateInvoiceAsync(_currentUser.TenantId, _currentUser.UserId, request, ct);
        return CreatedAtAction(nameof(Get), new { invoiceId = result.Value.Id }, ApiResponse<InvoiceResponse>.Ok(result.Value));
    }

    [HttpPatch("{invoiceId}")]
    public async Task<IActionResult> Update(string invoiceId, [FromBody] UpdateInvoiceRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateInvoiceAsync(invoiceId, _currentUser.TenantId, request, ct);
        if (result.IsFailure) return NotFound(ApiResponse<InvoiceResponse>.Fail(result.Error.Description));
        return Ok(ApiResponse<InvoiceResponse>.Ok(result.Value));
    }

    [HttpDelete("{invoiceId}")]
    public async Task<IActionResult> Delete(string invoiceId, CancellationToken ct)
    {
        var result = await _service.DeleteInvoiceAsync(invoiceId, _currentUser.TenantId, ct);
        if (result.IsFailure) return NotFound(ApiResponse.Fail(result.Error.Description));
        return NoContent();
    }
}
