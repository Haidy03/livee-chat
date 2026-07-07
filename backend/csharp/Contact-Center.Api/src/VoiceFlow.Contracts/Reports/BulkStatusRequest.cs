

namespace VoiceFlow.Contracts.Reports
{

    public sealed class BulkStatusRequest
    {
        public List<string> Ids { get; set; } = new();
        public string Status { get; set; } = "Paused";
    }

}
