using Contact_Center.Worker.Events.CallEvent.Options;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using OpenAI.Chat;
using Azure.AI.OpenAI;
using System.ClientModel;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using VoiceFlow.Contracts.Calls;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Common;


namespace Contact_Center.Worker.Events.CallEvent.Services
{
    public sealed class CallAnalysisService
    {
        private readonly CallAnalysisOptions _options;
        private readonly ILogger<CallAnalysisService> _logger;
        private readonly ChatClient _chatClient;

        public CallAnalysisService(IOptions<CallAnalysisOptions> options, ILogger<CallAnalysisService> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var azureOpenAi = new AzureOpenAIClient(
                new Uri(_options.OpenAiEndpoint),
                new ApiKeyCredential(_options.OpenAiKey));

            _chatClient = azureOpenAi.GetChatClient(_options.OpenAiDeployment);
        }

        // -- Orchestration ------------------------------------------------------

        public async Task<CallAnalysisResult> AnalyzeCallAsync(string wavFilePath, CancellationToken ct = default)
        {
            if (!File.Exists(wavFilePath))
                throw new FileNotFoundException("WAV file not found.", wavFilePath);

            _logger.LogInformation("Starting call analysis for {File}", wavFilePath);

            var segments = await TranscribeAsync(wavFilePath, ct);
            var transcript = FormatTranscript(segments);

            if (string.IsNullOrWhiteSpace(transcript))
            {
                _logger.LogWarning("Transcription produced no text for {File}", wavFilePath);
                return new CallAnalysisResult { Segments = segments };
            }

            // Summary and sentiment are independent, so run them concurrently.
            var summaryTask = SummarizeAsync(transcript, ct);
            var sentimentTask = AnalyzeSentimentAsync(transcript, ct);
            await Task.WhenAll(summaryTask, sentimentTask);

            return new CallAnalysisResult
            {
                Transcript = transcript,
                Segments = segments,
                Summary = summaryTask.Result,
                Sentiment = sentimentTask.Result
            };
        }

        // -- 1. Conversation transcription (with speaker diarization) -----------

        public async Task<IReadOnlyList<TranscriptSegment>> TranscribeAsync(
            string wavFilePath, CancellationToken ct = default)
        {
            var speechConfig = SpeechConfig.FromSubscription(_options.SpeechKey, _options.SpeechRegion);
            speechConfig.SpeechRecognitionLanguage = _options.Language;

            using var audioConfig = AudioConfig.FromWavFileInput(wavFilePath);
            using var transcriber = new ConversationTranscriber(speechConfig, audioConfig);

            var segments = new List<TranscriptSegment>();
            var stopRecognition = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            transcriber.Transcribed += (_, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech &&
                    !string.IsNullOrWhiteSpace(e.Result.Text))
                {
                    segments.Add(new TranscriptSegment
                    {
                        Speaker = string.IsNullOrEmpty(e.Result.SpeakerId) ? "Unknown" : e.Result.SpeakerId,
                        Text = e.Result.Text.Trim(),
                        Offset = TimeSpan.FromTicks(e.Result.OffsetInTicks),
                        Duration = e.Result.Duration
                    });
                }
            };

            transcriber.Canceled += (_, e) =>
            {
                if (e.Reason == CancellationReason.Error)
                    _logger.LogError("Transcription error {Code}: {Details}", e.ErrorCode, e.ErrorDetails);
                stopRecognition.TrySetResult(true);
            };

            transcriber.SessionStopped += (_, _) => stopRecognition.TrySetResult(true);

            await transcriber.StartTranscribingAsync().ConfigureAwait(false);

            // Honour caller cancellation.
            await using (ct.Register(() => stopRecognition.TrySetCanceled(ct)))
            {
                await stopRecognition.Task.ConfigureAwait(false);
            }

            await transcriber.StopTranscribingAsync().ConfigureAwait(false);

            return segments
                .OrderBy(s => s.Offset)
                .ToList();
        }

        // -- 2. Call summary ----------------------------------------------------

        public async Task<string> SummarizeAsync(string transcript, CancellationToken ct = default)
        {
            var messages = new ChatMessage[]
            {
            new SystemChatMessage(
                "You analyze customer-support phone calls. Produce a concise summary, the summary language same as the transcript language " +
                "covering: the customer's reason for calling, key points discussed, the " +
                "resolution or outcome, and any follow-up actions. Use clear, neutral prose."),
            new UserChatMessage($"Call transcript:\n\n{transcript}")
            };

            ChatCompletion completion = await _chatClient
                .CompleteChatAsync(messages, cancellationToken: ct)
                .ConfigureAwait(false);

            return completion.Content[0].Text.Trim();
        }

        // -- 3. Sentiment analysis ---------------------------------------------

        public async Task<SentimentResult> AnalyzeSentimentAsync(string transcript, CancellationToken ct = default)
        {
            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var messages = new ChatMessage[]
            {
            new SystemChatMessage(
                "You perform sentiment analysis on customer-support call transcripts. " +
                "Return ONLY a JSON object with this exact shape:\n" +
                "{\n" +
                "  \"overall\": \"Positive|Neutral|Negative|Mixed\",\n" +
                "  \"positive\": 0.0, \"neutral\": 0.0, \"negative\": 0.0,\n" +
                "  \"perSpeaker\": [{ \"speaker\": \"Guest-1\", \"sentiment\": \"Positive|Neutral|Negative|Mixed\" }]\n" +
                "}\n" +
                "The positive/neutral/negative values are confidence scores that sum to 1.0."),
            new UserChatMessage($"Call transcript:\n\n{transcript}")
            };

            ChatCompletion completion = await _chatClient
                .CompleteChatAsync(messages, options, ct)
                .ConfigureAwait(false);

            return ParseSentiment(completion.Content[0].Text);
        }

        // -- Helpers ------------------------------------------------------------

        private static string FormatTranscript(IEnumerable<TranscriptSegment> segments)
        {
            var sb = new StringBuilder();
            foreach (var s in segments)
                sb.AppendLine($"{s.Speaker}: {s.Text}");
            return sb.ToString().Trim();
        }

        private SentimentResult ParseSentiment(string json)
        {
            try
            {
                var dto = JsonSerializer.Deserialize<SentimentDto>(json, JsonOpts);
                if (dto is null)
                    return new SentimentResult();

                return new SentimentResult
                {
                    Overall = dto.Overall ?? "Neutral",
                    Positive = dto.Positive,
                    Neutral = dto.Neutral,
                    Negative = dto.Negative,
                    PerSpeaker = (dto.PerSpeaker ?? [])
                        .Select(p => new SpeakerSentiment
                        {
                            Speaker = p.Speaker ?? string.Empty,
                            Sentiment = p.Sentiment ?? "Neutral"
                        })
                        .ToList()
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse sentiment JSON: {Json}", json);
                return new SentimentResult();
            }
        }

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Internal DTOs for deserializing the model's JSON response.
        private sealed class SentimentDto
        {
            [JsonPropertyName("overall")] public string? Overall { get; set; }
            [JsonPropertyName("positive")] public double Positive { get; set; }
            [JsonPropertyName("neutral")] public double Neutral { get; set; }
            [JsonPropertyName("negative")] public double Negative { get; set; }
            [JsonPropertyName("perSpeaker")] public List<SpeakerDto>? PerSpeaker { get; set; }
        }

        private sealed class SpeakerDto
        {
            [JsonPropertyName("speaker")] public string? Speaker { get; set; }
            [JsonPropertyName("sentiment")] public string? Sentiment { get; set; }
        }
    }
}
