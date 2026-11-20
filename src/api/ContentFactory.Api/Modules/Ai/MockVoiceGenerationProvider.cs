using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ContentFactory.Api.Modules.Ai;

public class MockVoiceGenerationProvider(ILogger<MockVoiceGenerationProvider> logger) : IVoiceGenerationProvider
{
    public string ProviderName => "Mock";

    public string ModelOrWorkflowIdentifier => "mock-voice-synthesis";

    public IReadOnlyList<string> SupportedAudioFormats => ["audio/wav"];

    public async Task<VoiceGenerationResult> GenerateVoiceAssetAsync(
        VoiceGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Mock voice generation started for requirement {RequirementId}, candidates {Count}",
            request.AssetRequirementId, request.CandidateCount);

        // Simulate async latency
        await Task.Delay(200, cancellationToken);

        var textLower = (request.NarrationText ?? "").ToLowerInvariant();
        var guidanceLower = (request.PronunciationGuidance ?? "").ToLowerInvariant();

        // Check failure triggers
        if (textLower.Contains("[mock:timeout]"))
        {
            sw.Stop();
            return new VoiceGenerationResult(
                Success: false,
                Outputs: [],
                ErrorCode: "PROVIDER_TIMEOUT",
                SanitizedErrorMessage: "Simulated synthesis timeout (HTTP 504 / gateway timeout) from voice provider.",
                IsRetryable: true,
                ExecutionDurationMs: sw.ElapsedMilliseconds,
                EstimatedCostUsd: 0.001m,
                ActualCostUsd: null
            );
        }

        if (textLower.Contains("[mock:retryable-failure]") ||
            guidanceLower.Contains("[mock:retryable-failure]"))
        {
            sw.Stop();
            return new VoiceGenerationResult(
                Success: false,
                Outputs: [],
                ErrorCode: "PROVIDER_TRANSIENT_503",
                SanitizedErrorMessage: "Simulated transient HTTP 503 from mock voice provider.",
                IsRetryable: true,
                ExecutionDurationMs: sw.ElapsedMilliseconds,
                EstimatedCostUsd: 0.001m,
                ActualCostUsd: null
            );
        }

        if (textLower.Contains("[mock:action-required-failure]") ||
            textLower.Contains("[mock:auth-failure]") ||
            guidanceLower.Contains("[mock:action-required-failure]"))
        {
            sw.Stop();
            return new VoiceGenerationResult(
                Success: false,
                Outputs: [],
                ErrorCode: "INVALID_VOICE_CONFIGURATION",
                SanitizedErrorMessage: "Simulated invalid voice profile configuration or missing credentials. Action required.",
                IsRetryable: false,
                ExecutionDurationMs: sw.ElapsedMilliseconds,
                EstimatedCostUsd: 0.001m,
                ActualCostUsd: null
            );
        }

        // Calculate realistic duration from word count
        var words = (request.NarrationText ?? "")
            .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Length;

        // Default to ~140 WPM (2.33 words/sec), min duration 2.5s
        var pacingWpm = 140;
        var durationSeconds = Math.Max(2.5, Math.Round(words / (pacingWpm / 60.0), 1));
        var sampleRate = 24000;
        var candidateCount = Math.Clamp(request.CandidateCount, 1, 2);

        var outputs = new List<VoiceGeneratedMediaOutput>();

        for (int i = 1; i <= candidateCount; i++)
        {
            // Vary pitch slightly per candidate (e.g. 440 Hz for var 1, 480 Hz for var 2)
            var toneFrequency = i == 1 ? 440.0 : 480.0;
            // Slight variation in duration for multi-candidate comparison
            var candidateDuration = i == 1 ? durationSeconds : Math.Round(durationSeconds * 1.03, 1);
            var wavBytes = GeneratePcmWav(candidateDuration, sampleRate, toneFrequency);

            var paramSnapshot = JsonSerializer.Serialize(new
            {
                provider = "Mock",
                profileKey = request.VoiceProfile.ProfileKey,
                profileName = request.VoiceProfile.ProfileName,
                language = request.Language,
                configuredLocale = request.ConfiguredLocale ?? request.VoiceProfile.ConfiguredLocale,
                speakingStyle = request.VoiceProfile.SpeakingStyle,
                tone = request.VoiceProfile.Tone,
                pace = request.VoiceProfile.Pace,
                pronunciationGuidance = request.PronunciationGuidance,
                sampleRateHz = sampleRate,
                channels = 1,
                bitDepth = 16,
                variantIndex = i,
                model = "mock-tts-es-v1"
            });

            outputs.Add(new VoiceGeneratedMediaOutput(
                VariantIndex: i,
                AudioBytes: wavBytes,
                ContentType: "audio/wav",
                FileExtension: "wav",
                DurationSeconds: candidateDuration,
                SampleRateHz: sampleRate,
                Channels: 1,
                BitDepth: 16,
                ProviderModel: "mock-tts-es-v1",
                GenerationParametersSnapshot: paramSnapshot
            ));
        }

        sw.Stop();
        return new VoiceGenerationResult(
            Success: true,
            Outputs: outputs,
            ErrorCode: null,
            SanitizedErrorMessage: null,
            IsRetryable: false,
            ExecutionDurationMs: sw.ElapsedMilliseconds,
            EstimatedCostUsd: 0.0015m * candidateCount,
            ActualCostUsd: 0.0015m * candidateCount,
            Provider: ProviderName,
            IsSimulated: true
        );
    }

    public static byte[] GeneratePcmWav(double durationSeconds, int sampleRate = 24000, double frequency = 440.0)
    {
        var numSamples = (int)(sampleRate * durationSeconds);
        var pcmBytes = new byte[numSamples * 2]; // 16-bit = 2 bytes per sample

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / sampleRate;
            // Smooth attack and release envelope (50ms fade-in/out)
            double envelope = Math.Min(1.0, Math.Min(t / 0.05, (durationSeconds - t) / 0.05));
            if (envelope < 0) envelope = 0;
            short sample = (short)(Math.Sin(2.0 * Math.PI * frequency * t) * envelope * 8000.0);

            pcmBytes[i * 2] = (byte)(sample & 0xFF);
            pcmBytes[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // RIFF chunk header
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + pcmBytes.Length);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));

        // fmt subchunk
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16); // Subchunk1Size = 16 for PCM
        writer.Write((short)1); // AudioFormat = 1 (PCM)
        writer.Write((short)1); // NumChannels = 1 (Mono)
        writer.Write(sampleRate); // SampleRate
        writer.Write(sampleRate * 2); // ByteRate = SampleRate * NumChannels * BitsPerSample/8
        writer.Write((short)2); // BlockAlign = NumChannels * BitsPerSample/8
        writer.Write((short)16); // BitsPerSample = 16

        // data subchunk
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(pcmBytes.Length);
        writer.Write(pcmBytes);

        return ms.ToArray();
    }
}
