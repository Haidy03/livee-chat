using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Api.SkillCatalog.Requests;
using VoiceFlow.Api.SkillCatalog.Responses;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces.SkillCatalog;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.SkillCatalog.Requests;
namespace VoiceFlow.Reports.Api.Controllers;

/// <summary>Skills catalog management (categories and their options).</summary>
[ApiController]
[Authorize]
[Route("api/v1/skills/catalog")]
[Produces("application/json")]
public sealed class SkillsCatalogController : ControllerBase
{
    private readonly ISkillCatalogService _service;
    private readonly ITenantContext _tenant;

    public SkillsCatalogController(ISkillCatalogService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SkillCategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SkillCategoryResponse>>>> List(CancellationToken ct)
    {
        var data = await _service.ListAsync(_tenant.TenantId, ct);
        return Ok(ApiResponse<IReadOnlyList<SkillCategoryResponse>>.Ok(data));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SkillCategoryResponse>>>> ReplaceAll(
        [FromBody] SaveSkillCatalogRequest request, CancellationToken ct)
    {
        try
        {
            var data = await _service.ReplaceAllAsync(_tenant.TenantId, request, ct);
            return Ok(ApiResponse<IReadOnlyList<SkillCategoryResponse>>.Ok(data));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<IReadOnlyList<SkillCategoryResponse>>.Fail(ex.Message));
        }
    }

    [HttpPost("categories")]
    public async Task<ActionResult<ApiResponse<SkillCategoryResponse>>> CreateCategory(
        [FromBody] UpsertSkillCategoryRequest request, CancellationToken ct)
    {
        try
        {
            var data = await _service.CreateCategoryAsync(_tenant.TenantId, request, ct);
            return Ok(ApiResponse<SkillCategoryResponse>.Ok(data));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<SkillCategoryResponse>.Fail(ex.Message));
        }
    }

    [HttpPut("categories/{categoryId}")]
    public async Task<ActionResult<ApiResponse<SkillCategoryResponse>>> UpdateCategory(
        string categoryId, [FromBody] UpsertSkillCategoryRequest request, CancellationToken ct)
    {
        try
        {
            var data = await _service.UpdateCategoryAsync(_tenant.TenantId, categoryId, request, ct);
            return Ok(ApiResponse<SkillCategoryResponse>.Ok(data));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SkillCategoryResponse>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<SkillCategoryResponse>.Fail(ex.Message));
        }
    }

    [HttpDelete("categories/{categoryId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(string categoryId, CancellationToken ct)
    {
        var ok = await _service.DeleteCategoryAsync(_tenant.TenantId, categoryId, ct);
        if (!ok) return NotFound(ApiResponse<object>.Fail("notfound"));
        return Ok(ApiResponse<object>.Ok(new { ok = true }));
    }

    [HttpPost("categories/{categoryId}/options")]
    public async Task<ActionResult<ApiResponse<SkillOptionResponse>>> AddOption(
        string categoryId, [FromBody] UpsertSkillOptionRequest request, CancellationToken ct)
    {
        try
        {
            var data = await _service.AddOptionAsync(_tenant.TenantId, categoryId, request, ct);
            return Ok(ApiResponse<SkillOptionResponse>.Ok(data));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SkillOptionResponse>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<SkillOptionResponse>.Fail(ex.Message));
        }
    }

    [HttpPut("categories/{categoryId}/options/{optionId}")]
    public async Task<ActionResult<ApiResponse<SkillOptionResponse>>> UpdateOption(
        string categoryId, string optionId, [FromBody] UpsertSkillOptionRequest request, CancellationToken ct)
    {
        try
        {
            var data = await _service.UpdateOptionAsync(_tenant.TenantId, categoryId, optionId, request, ct);
            return Ok(ApiResponse<SkillOptionResponse>.Ok(data));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SkillOptionResponse>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<SkillOptionResponse>.Fail(ex.Message));
        }
    }

    [HttpDelete("categories/{categoryId}/options/{optionId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteOption(
        string categoryId, string optionId, CancellationToken ct)
    {
        var ok = await _service.DeleteOptionAsync(_tenant.TenantId, categoryId, optionId, ct);
        if (!ok) return NotFound(ApiResponse<object>.Fail("notfound"));
        return Ok(ApiResponse<object>.Ok(new { ok = true }));
    }
}
