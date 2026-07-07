using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Common
{

    public sealed class SpeakerSentiment
    {
        public string Speaker { get; init; } = string.Empty;
        public string Sentiment { get; init; } = "Neutral";
    }

    public sealed class SentimentResult
    {
        /// <summary>Positive | Neutral | Negative | Mixed</summary>
        public string Overall { get; init; } = "Neutral";
        public double Positive { get; init; }
        public double Neutral { get; init; }
        public double Negative { get; init; }
        public IReadOnlyList<SpeakerSentiment> PerSpeaker { get; init; } = [];
    }

    public sealed class CallAnalysisResult
    {
        /// <summary>Full conversation with speaker labels, one line per segment.</summary>
        public string Transcript { get; init; } = string.Empty;
        public IReadOnlyList<TranscriptSegment> Segments { get; init; } = [];
        public string Summary { get; init; } = string.Empty;
        public SentimentResult Sentiment { get; init; } = new();
    }
}
