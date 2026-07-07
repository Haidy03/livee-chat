using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.SipAccounts;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Application.Services;

public sealed class SipAccountService : ISipAccountService
{
    private readonly ISipAccountRepository _sipRepository;
    private readonly ISoftphoneCallLogRepository _logRepository;
    private readonly IConfiguration _configuration;
    

    public SipAccountService(ISipAccountRepository sipRepository,
        ISoftphoneCallLogRepository logRepository, IConfiguration configuration)
    {
        _sipRepository = sipRepository;
        _logRepository = logRepository;
        _configuration = configuration;
    }

    public async Task<Result<SipAccountResponse>> GetSipAccountAsync(string userId, string tenantId, CancellationToken cancellationToken = default)
    {
        var sip = await _sipRepository.GetByUserAndTenantAsync(userId, tenantId, cancellationToken);
        if (sip is null)
            return Result.Failure<SipAccountResponse>(Error.NotFound("SipAccount", userId));

        if (string.IsNullOrWhiteSpace(sip.WsUrl))
            sip.WsUrl = _configuration["Websocket:Uri"] ?? "wss://pbx.alkhwarizmi.cloud:8089/ws";
        if (sip.StunUrls is null || sip.StunUrls.Count == 0)
            sip.StunUrls = new List<string> { _configuration["Websocket:stunUrl"] ?? "stun:stun.l.google.com:19302" };
        return MapToResponse(sip);
    }

    public async Task<Result<SipAccountResponse>> CreateSipAccountAsync(string userId, string tenantId, CreateSipAccountRequest request, CancellationToken cancellationToken = default)
    {
        var sip = new SipAccount { UserId = userId, TenantId = tenantId, DisplayName = request.DisplayName, SipUri = request.SipUri, AuthId = request.AuthId, WsUrl = request.WsUrl, StunUrls = request.StunUrls, TurnUrl = request.TurnUrl, TurnUsername = request.TurnUsername };
        await _sipRepository.InsertAsync(sip, cancellationToken);
        return MapToResponse(sip);
    }

    public async Task<Result<SipAccountResponse>> UpdateSipAccountAsync(string userId, string tenantId, UpdateSipAccountRequest request, CancellationToken cancellationToken = default)
    {
        var sip = await _sipRepository.GetByUserAndTenantAsync(userId, tenantId, cancellationToken);
        if (sip is null)
            sip = new SipAccount { Id=ObjectId.GenerateNewId().ToString(), UserId = userId, TenantId = tenantId };


        sip.DisplayName = request?.DisplayName??sip.DisplayName;
        sip.WsUrl = request?.WsUrl ?? sip.WsUrl;
        sip.StunUrls = request?.StunUrls ?? sip.StunUrls;
        sip.TurnUrl = request?.TurnUrl ?? sip.TurnUrl;
        sip.TurnUsername = request?.TurnUsername ?? sip.TurnUsername;
        sip.IsActive = request?.IsActive ?? sip.IsActive;
        sip.AuthId = request?.AuthId ?? sip.AuthId;
        sip.SipUri = request?.SipUri ?? sip.SipUri;

        await _sipRepository.UpdateWithUpsertAsync(sip, cancellationToken);
        return MapToResponse(sip);
    }

    public async Task<Result<PagedResponse<SoftphoneCallLogResponse>>> GetSoftphoneCallLogsAsync(string userId, string tenantId, PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _logRepository.GetByUserAndTenantAsync(userId, tenantId, pagination.Skip, pagination.PageSize, cancellationToken);
        return PagedResponse<SoftphoneCallLogResponse>.Create(items.Select(MapLogToResponse).ToList().AsReadOnly(), pagination.Page, pagination.PageSize, total);
    }

    public async Task<Result<SoftphoneCallLogResponse>> CreateSoftphoneCallLogAsync(string userId, string tenantId, CreateSoftphoneCallLogRequest request, CancellationToken cancellationToken = default)
    {
        var log = new SoftphoneCallLog { UserId = userId, TenantId = tenantId, Direction = request.Direction, Status = request.Status, Number = request.Number, DisplayName = request.DisplayName, ContactId = request.ContactId, StartedAt = request.StartedAt, DurationSec = request.DurationSec, FailureReason = request.FailureReason };
        await _logRepository.InsertAsync(log, cancellationToken);
        return MapLogToResponse(log);
    }

    private static SipAccountResponse MapToResponse(SipAccount s) => new() { Id = s.Id, UserId = s.UserId, DisplayName = s.DisplayName, SipUri = s.SipUri, AuthId = s.AuthId, WsUrl = s.WsUrl, StunUrls = s.StunUrls, TurnUrl = s.TurnUrl, IsActive = s.IsActive, PPXPassword = s.PPXPassword };
    private static SoftphoneCallLogResponse MapLogToResponse(SoftphoneCallLog l) => new() { Id = l.Id, Direction = l.Direction, Status = l.Status, Number = l.Number, DisplayName = l.DisplayName, ContactId = l.ContactId, StartedAt = l.StartedAt, DurationSec = l.DurationSec, FailureReason = l.FailureReason };
}
