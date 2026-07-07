namespace VoiceFlow.Contracts.Events
{
    public class CallTerminatedEvent
    {
        public string? Id { get; set; }
        public string? CallId { get; set; }
        public string? TenantId { get; set; }
        public string? StoragePath { get; set; }
        public DateTime? Timestamp { get; set; }
        public string Event { get; set; } = "AudioTerminated";

        // Voicemail ("VoicemailRecorded") fields. Reuses this message class the same way
        // "VoicePublished" does; the worker switches on Event. Ignored for call events.
        public string? OwnerType { get; set; }     // queue | group | agent | flow
        public string? OwnerId { get; set; }        // destination mailbox owner (NOT the caller)
        public string? RecordingPath { get; set; }  // local path on the Asterisk box
        public bool Transcription { get; set; }      // run transcription for this voicemail
    }
}
