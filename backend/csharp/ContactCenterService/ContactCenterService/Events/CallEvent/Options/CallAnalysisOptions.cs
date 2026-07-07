using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contact_Center.Worker.Events.CallEvent.Options
{
    public sealed class CallAnalysisOptions
    {
        public const string SectionName = "CallAnalysis";
        // Azure Speech (Cognitive Services)
        public string SpeechKey { get; set; } = string.Empty;
        public string SpeechRegion { get; set; } = string.Empty;

        /// <summary>BCP-47 language code of the call, e.g. "en-US", "ar-EG".</summary>
        public string Language { get; set; } = "en-US";

        // Azure OpenAI
        public string OpenAiEndpoint { get; set; } = string.Empty;
        public string OpenAiKey { get; set; } = string.Empty;

        /// <summary>The chat model deployment name, e.g. "gpt-4o".</summary>
        public string OpenAiDeployment { get; set; } = "gpt-4o";
    }
}
