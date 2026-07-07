using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces.Surveys;
using VoiceFlow.Application.Services.Surveys;
using VoiceFlow.Contracts.Surveys;
using VoiceFlow.Core.Entities.Surveys;
using VoiceFlow.Core.Enums.Survey;
using VoiceFlow.Core.Exceptions.Surveys;
using VoiceFlow.Core.Interfaces.Repositories.Surveys;


namespace VoiceFlow.Surveys.Application;

public sealed class SurveyService : ISurveyService
{
    private readonly ISurveyRepository _repo;
    private readonly ISurveyResponseRepository _responses;
    private readonly ITenantContext _tenant;
    private readonly IClock _clock;

    public SurveyService(ISurveyRepository repo, ISurveyResponseRepository responses, ITenantContext tenant, IClock clock)
    {
        _repo = repo; _responses = responses; _tenant = tenant; _clock = clock;
    }

    public Task<IReadOnlyList<Survey>> ListAsync(string? search, SurveyStatus? status, SurveyLanguage? language, CancellationToken ct)
        => _repo.ListAsync(_tenant.TenantId, search, status, language, ct);

    public async Task<Survey> GetAsync(string id, CancellationToken ct)
        => await _repo.GetAsync(_tenant.TenantId, id, ct) ?? throw new SurveyNotFoundException(id);

    public async Task<Survey> CreateAsync(SurveyCreateRequest req, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var s = new Survey
        {
            TenantId = _tenant.TenantId,
            Name = req.Name, Description = req.Description, Language = req.Language, TtsVoice = req.TtsVoice,
            welcomeSound = req.welcomeSound, completionSound = req.completionSound,
            WebhookUrl = req.WebhookUrl, WebhookSecret = req.WebhookSecret,
            MaxRetries = req.MaxRetries, InputTimeoutSec = req.InputTimeoutSec,
            Abandonment = req.Abandonment, Status = req.Status,
            CreatedAt = now.UtcDateTime, UpdatedAt = now.UtcDateTime,
            Questions = req.Questions ?? new(), UsedInFlowIds = req.UsedInFlowIds ?? new(),
        };
        s.ValidateBranching();
        await _repo.AddAsync(s, ct);
        return s;
    }

    public async Task<Survey> UpdateAsync(string id, SurveyUpdateRequest req, CancellationToken ct)
    {
        var s = await GetAsync(id, ct);
        if (req.Name is not null) s.Name = req.Name;
        if (req.Description is not null) s.Description = req.Description;
        if (req.Language is not null) s.Language = req.Language.Value;
        if (req.TtsVoice is not null) s.TtsVoice = req.TtsVoice.Value;
        if (req.welcomeSound is not null) s.welcomeSound = req.welcomeSound;
        if (req.completionSound is not null) s.completionSound = req.completionSound;
        if (req.WebhookUrl is not null) s.WebhookUrl = req.WebhookUrl;
        if (req.WebhookSecret is not null) s.WebhookSecret = req.WebhookSecret;
        if (req.MaxRetries is not null) s.MaxRetries = req.MaxRetries.Value;
        if (req.InputTimeoutSec is not null) s.InputTimeoutSec = req.InputTimeoutSec.Value;
        if (req.Abandonment is not null) s.Abandonment = req.Abandonment.Value;
        if (req.Status is not null) s.SetStatus(req.Status.Value);
        if (req.Questions is not null) s.Questions = req.Questions;
        if (req.UsedInFlowIds is not null) s.UsedInFlowIds = req.UsedInFlowIds;
        s.UpdatedAt = _clock.UtcNow.UtcDateTime;
        s.ValidateBranching();
        await _repo.UpdateSurveyAsync(s, ct);
        return s;
    }

    public async Task<Survey> SetStatusAsync(string id, SurveyStatus status, CancellationToken ct)
    {
        var s = await GetAsync(id, ct);
        s.SetStatus(status);
        s.UpdatedAt = _clock.UtcNow.UtcDateTime;
        await _repo.UpdateSurveyAsync(s, ct);
        return s;
    }

    public async Task<Survey> DuplicateAsync(string id, CancellationToken ct)
    {
        var src = await GetAsync(id, ct);
        var copy = src.Duplicate(Guid.NewGuid(), _clock.UtcNow);
        await _repo.AddAsync(copy, ct);
        return copy;
    }

    public async Task DeleteAsync(string id, CancellationToken ct)
    {
        var ok = await _repo.DeleteAsync(_tenant.TenantId, id, ct);
        if (!ok) throw new SurveyNotFoundException(id);
    }

    public async Task<IReadOnlyList<SurveyResponse>> ListResponsesAsync(string id, int limit, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        _ = await GetAsync(id, ct);
        return await _responses.ListAsync(_tenant.TenantId, id, limit, from, to, ct);
    }

    public async Task<SurveyWebhookResult> IngestWebhookAsync(
         SurveyWebhookPayload payload, string rawBody, string? signatureHeader, CancellationToken ct)
    {
        string surveyId = payload.SurveyId;
        // Anonymous endpoint: load survey across all tenants by id.
        var survey = await _repo.GetSurveyByIdAsync(surveyId, ct)
            ?? throw new SurveyNotFoundException(surveyId);

        // HMAC validation (skipped when survey has no secret).
        if (!WebhookSignatureValidator.IsValid(rawBody, signatureHeader, survey.WebhookSecret))
            throw new InvalidWebhookSignatureException();

        // Idempotency on call_id.
        if (!string.IsNullOrEmpty(payload.CallId))
        {
            var existing = await _responses.GetByCallIdAsync(surveyId, payload.CallId!, ct);
            if (existing is not null) return new SurveyWebhookResult(existing, Duplicate: true);
        }

        var answers = new Dictionary<string, string?>();
        foreach (var a in payload.Answers ?? new())
        {
            if (string.IsNullOrEmpty(a.QuestionId)) continue;
            answers[a.QuestionId] = a.Answer;
        }

        var startedAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(payload.StartedAt!));

        var endedAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(payload.EndedAt!));

        var response = new SurveyResponse
        {
            SurveyId = surveyId,
            TenantId = survey.TenantId,
            CallId = payload.CallId,
            CallerPhone = payload.PhoneNumber ?? "",
            At = endedAt,
            StartedAt = startedAt,
            EndedAt = endedAt,
            DurationSeconds = payload.DurationSeconds,
            CompletionStatus = payload.CompletionStatus,
            Language = payload.Language,
            Completed = string.Equals(payload.CompletionStatus, "complete", StringComparison.OrdinalIgnoreCase),
            CustomFields = payload.CustomFields ?? new(),
            PassedVariables = payload.PassedVariables ?? new(),
            Answers = answers,
        };

        await _responses.InsertSurveyResponseAsync(response, ct);
        return new SurveyWebhookResult(response, Duplicate: false);
    }
}
