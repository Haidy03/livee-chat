using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.FreeSwitchXmlCurl.Models;
using VoiceFlow.FreeSwitchXmlCurl.Services;

namespace VoiceFlow.FreeSwitchXmlCurl.Controllers;

[ApiController]
[Route("api/freeswitch")]
public sealed class VoicemailController : ControllerBase
{
    private readonly IVoicemailService _voicemail;
    private readonly ILogger<VoicemailController> _log;

    public VoicemailController(IVoicemailService voicemail, ILogger<VoicemailController> log)
    {
        _voicemail = voicemail;
        _log = log;
    }

    public sealed class VoicemailRecordedRequest
    {
        [Required] public string Uuid { get; set; } = default!;
        [Required] public string Mailbox { get; set; } = default!;
        [Required] public string Domain { get; set; } = default!;
        public string? Context { get; set; }
        public string? TenantId { get; set; }
        public string? FlowId { get; set; }
        public string? NodeId { get; set; }
        public string? CallerIdNumber { get; set; }
        public string? DestinationNumber { get; set; }
        public string? RecordingPath { get; set; }
        public string? FileSize { get; set; }
        public string? Format { get; set; }
        public string? Duration { get; set; }
        public string? Timestamp { get; set; }
        public string? VmMessageExt { get; set; }
    }

    [HttpPost("voicemail-recorded")]
    [Consumes("application/json")]
    public async Task<IActionResult> Recorded([FromBody] VoicemailRecordedRequest req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new { error = "missing body" });
        if (string.IsNullOrWhiteSpace(req.Uuid) ||
            string.IsNullOrWhiteSpace(req.Mailbox) ||
            string.IsNullOrWhiteSpace(req.Domain))
        {
            return BadRequest(new { error = "uuid, mailbox and domain are required" });
        }

        _log.LogInformation(
            "Voicemail webhook received uuid={Uuid} mailbox={Mailbox} domain={Domain}",
            req.Uuid, req.Mailbox, req.Domain);

        long? fileSize = long.TryParse(req.FileSize, out var fs) ? fs : null;
        int duration = int.TryParse(req.Duration, out var dur) ? dur : 0;
        DateTime timestamp = DateTime.TryParse(req.Timestamp, null,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
            out var ts) ? ts : DateTime.UtcNow;

        var message = new VoicemailMessage
        {
            Uuid = req.Uuid,
            Mailbox = req.Mailbox,
            Domain = req.Domain,
            Context = NullIfBlank(req.Context),
            TenantId = NullIfBlank(req.TenantId),
            FlowId = NullIfBlank(req.FlowId),
            NodeId = NullIfBlank(req.NodeId),
            CallerIdNumber = NullIfBlank(req.CallerIdNumber),
            DestinationNumber = NullIfBlank(req.DestinationNumber),
            RecordingPath = NullIfBlank(req.RecordingPath),
            FileSize = fileSize,
            Format = string.IsNullOrWhiteSpace(req.Format) ? "wav" : req.Format!,
            DurationSeconds = duration,
            VmMessageExt = NullIfBlank(req.VmMessageExt),
            Timestamp = timestamp,
            CreatedAt = DateTime.UtcNow,
        };

        var id = await _voicemail.RecordAsync(message, ct);
        return NoContent();
    }

    private static string? NullIfBlank(string? v) => string.IsNullOrWhiteSpace(v) ? null : v;
}
