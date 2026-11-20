using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ContentFactory.Api.Modules.Ai;

public class GeminiTtsProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<GeminiTtsProvider> logger) : IVoiceGenerationProvider
{
    public string ProviderName => "Gemini";

    public string ModelOrWorkflowIdentifier => _model;

    public IReadOnlyList<string> SupportedAudioFormats => ["audio/wav"];

    private readonly string _apiKey = configuration["GEMINI_API_KEY"] ?? configuration["Gemini:ApiKey"] ?? string.Empty;
    private readonly string _model = configuration["GEMINI_TTS_MODEL"] ?? configuration["Gemini:TtsModel"] ?? "gemini-3.8-flash-lite-tts";
    private readonly string _defaultVoice = configuration["GEMINI_VOICE_NAME"] ?? configuration["Gemini:VoiceName"] ?? "Kore";

    public async Task<VoiceGenerationResult> GenerateVoiceAssetAsync(
        VoiceGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "CHANGE_ME")
        {
            sw.Stop();
            return new VoiceGenerationResult(
                Success: false,
                Outputs: [],
                ErrorCode: "MISSING_API_KEY",
                SanitizedErrorMessage: "Gemini API key is not configured in backend settings.",
                IsRetryable: false,
                ExecutionDurationMs: sw.ElapsedMilliseconds,
                EstimatedCostUsd: 0m,
                ActualCostUsd: null
            );
        }

        var candidateCount = Math.Clamp(request.CandidateCount, 1, 2);
        var outputs = new List<VoiceGeneratedMediaOutput>();
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(120);

        var voiceName = ResolveVoiceName(request.VoiceProfile, request.ConfiguredLocale);

        for (int i = 1; i <= candidateCount; i++)
        {
            try
            {
                // Variant 2 can use alternate voice if configured, or default
                var currentVoice = i == 2 ? ResolveAlternateVoiceName(voiceName) : voiceName;
                var singleResult = await SynthesizeSingleCandidateAsync(
                    client, request, i, currentVoice, cancellationToken);

                if (!singleResult.Success)
                {
                    sw.Stop();
                    return new VoiceGenerationResult(
                        Success: false,
                        Outputs: [],
                        ErrorCode: singleResult.ErrorCode,
                        SanitizedErrorMessage: singleResult.ErrorMessage,
                        IsRetryable: singleResult.IsRetryable,
                        ExecutionDurationMs: sw.ElapsedMilliseconds,
                        EstimatedCostUsd: null,
                        ActualCostUsd: null
                    );
                }

                outputs.Add(singleResult.Output!);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                logger.LogError(ex, "Unexpected error in GeminiTtsProvider for Job {JobId}", request.JobId);
                return new VoiceGenerationResult(
                    Success: false,
                    Outputs: [],
                    ErrorCode: "PROVIDER_EXECUTION_ERROR",
                    SanitizedErrorMessage: "An unexpected error occurred while communicating with the voice provider.",
                    IsRetryable: true,
                    ExecutionDurationMs: sw.ElapsedMilliseconds,
                    EstimatedCostUsd: null,
                    ActualCostUsd: null
                );
            }
        }

        sw.Stop();
        // Estimated cost based on character count (~$0.0001 per 1000 chars)
        var charCount = (request.NarrationText ?? "").Length;
        var estCost = Math.Round((decimal)charCount / 1000m * 0.002m * candidateCount, 5);

        return new VoiceGenerationResult(
            Success: true,
            Outputs: outputs,
            ErrorCode: null,
            SanitizedErrorMessage: null,
            IsRetryable: false,
            ExecutionDurationMs: sw.ElapsedMilliseconds,
            EstimatedCostUsd: estCost,
            ActualCostUsd: estCost,
            Provider: ProviderName,
            IsSimulated: false
        );
    }

    private async Task<(bool Success, VoiceGeneratedMediaOutput? Output, string? ErrorCode, string? ErrorMessage, bool IsRetryable)>
        SynthesizeSingleCandidateAsync(
            HttpClient client,
            VoiceGenerationRequest request,
            int variantIndex,
            string voiceName,
            CancellationToken cancellationToken)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";

        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine($"You are a professional Spanish voice actor recording spoken narration.");
        promptBuilder.AppendLine($"Speaking style: {request.VoiceProfile.SpeakingStyle}. Tone: {request.VoiceProfile.Tone}. Pace: {request.VoiceProfile.Pace}.");

        if (!string.IsNullOrWhiteSpace(request.PronunciationGuidance))
        {
            promptBuilder.AppendLine($"Pronunciation guidance: {request.PronunciationGuidance}");
        }

        if (request.VoiceProfile.PronunciationHints != null && request.VoiceProfile.PronunciationHints.Count > 0)
        {
            promptBuilder.AppendLine("Pronunciation phonetic substitutions:");
            foreach (var (word, phonetic) in request.VoiceProfile.PronunciationHints)
            {
                promptBuilder.AppendLine($"- {word}: {phonetic}");
            }
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Read the following narration text aloud clearly and naturally without adding commentary or introductory phrases:");
        promptBuilder.AppendLine(request.NarrationText);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = promptBuilder.ToString() }
                    }
                }
            },
            generationConfig = new
            {
                responseModalities = new[] { "AUDIO" },
                speechConfig = new
                {
                    voiceConfig = new
                    {
                        prebuiltVoiceConfig = new
                        {
                            voiceName = voiceName
                        }
                    }
                }
            }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = jsonContent };
        httpRequest.Headers.Add("x-goog-api-key", _apiKey);
        using var response = await client.SendAsync(httpRequest, cancellationToken);
        var statusCode = (int)response.StatusCode;
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Gemini TTS API returned HTTP {StatusCode}: {ResponseBody}", statusCode, SanitizeForLog(responseBody));

            if (statusCode == 401 || statusCode == 403)
            {
                return (false, null, "PROVIDER_AUTH_FAILURE", "Voice provider authentication failed. Check configured API credentials.", false);
            }
            if (statusCode == 429)
            {
                return (false, null, "PROVIDER_RATE_LIMIT", "Voice provider rate limit exceeded. Retry scheduled.", true);
            }
            if (statusCode >= 500)
            {
                return (false, null, "PROVIDER_TRANSIENT_5xx", "Voice provider temporarily unavailable. Retry scheduled.", true);
            }

            return (false, null, "PROVIDER_REJECTED", "Voice provider rejected generation request. Inspect prompt or parameters.", false);
        }

        // Parse response to find inline audio bytes
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var candidates = root.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
            {
                return (false, null, "PROVIDER_EMPTY_RESPONSE", "Gemini returned zero response candidates.", false);
            }

            var parts = candidates[0].GetProperty("content").GetProperty("parts");
            string? base64Data = null;
            string? mimeType = null;

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("inlineData", out var inlineData))
                {
                    base64Data = inlineData.GetProperty("data").GetString();
                    mimeType = inlineData.GetProperty("mimeType").GetString();
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(base64Data))
            {
                return (false, null, "PROVIDER_NO_AUDIO_DATA", "Gemini response did not contain audio data.", false);
            }

            var rawBytes = Convert.FromBase64String(base64Data);
            byte[] wavBytes;
            int? sampleRate = ExtractSampleRateFromMimeType(mimeType);
            int channels;
            int bitDepth;
            double durationSeconds;

            // Inspect payload format via magic bytes and MIME type
            if (IsCompressedAudioFormat(rawBytes, mimeType))
            {
                logger.LogWarning("Gemini returned compressed audio format (MIME: {MimeType}). Raw PCM or WAV container expected.", mimeType);
                return (false, null, "PROVIDER_UNSUPPORTED_ENCODING", $"Voice provider returned compressed audio format '{mimeType}' instead of uncompressed PCM WAV.", false);
            }

            // Check if already valid WAV container (RIFF + WAVE header)
            if (IsWav(rawBytes))
            {
                if (!TryReadWavMetadata(rawBytes, out var wavMetadata, out var wavError))
                {
                    return (false, null, "PROVIDER_INVALID_WAV", wavError, false);
                }

                wavBytes = rawBytes;
                sampleRate = wavMetadata.SampleRateHz;
                channels = wavMetadata.Channels;
                bitDepth = wavMetadata.BitDepth;
                durationSeconds = Math.Round((double)wavMetadata.DataLength / wavMetadata.ByteRate, 2);
            }
            else
            {
                if (!sampleRate.HasValue)
                {
                    return (false, null, "PROVIDER_MISSING_SAMPLE_RATE", "Voice provider returned raw PCM without a sample rate.", false);
                }

                // Raw uncompressed PCM from Gemini: wrap in standard 16-bit mono WAV container
                channels = 1;
                bitDepth = 16;
                wavBytes = WrapPcmInWav(rawBytes, sampleRate.Value);
                var byteRate = sampleRate.Value * channels * (bitDepth / 8);
                durationSeconds = byteRate > 0 ? Math.Round((double)rawBytes.Length / byteRate, 2) : 0;
            }

            var paramSnapshot = JsonSerializer.Serialize(new
            {
                provider = "Gemini",
                model = _model,
                voiceName = voiceName,
                profileKey = request.VoiceProfile.ProfileKey,
                language = request.Language,
                configuredLocale = request.ConfiguredLocale ?? request.VoiceProfile.ConfiguredLocale,
                speakingStyle = request.VoiceProfile.SpeakingStyle,
                tone = request.VoiceProfile.Tone,
                pace = request.VoiceProfile.Pace,
                pronunciationGuidance = request.PronunciationGuidance,
                sampleRateHz = sampleRate!.Value,
                channels = channels,
                bitDepth = bitDepth,
                variantIndex = variantIndex
            });

            var output = new VoiceGeneratedMediaOutput(
                VariantIndex: variantIndex,
                AudioBytes: wavBytes,
                ContentType: "audio/wav",
                FileExtension: "wav",
                DurationSeconds: durationSeconds,
                SampleRateHz: sampleRate!.Value,
                Channels: channels,
                BitDepth: bitDepth,
                ProviderModel: $"{_model}:{voiceName}",
                GenerationParametersSnapshot: paramSnapshot
            );

            return (true, output, null, null, false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse Gemini TTS response payload");
            return (false, null, "PROVIDER_PARSE_ERROR", "Failed to parse provider audio response.", false);
        }
    }

    private static bool IsCompressedAudioFormat(byte[] bytes, string? mimeType)
    {
        if (!string.IsNullOrWhiteSpace(mimeType))
        {
            var lowerMime = mimeType.ToLowerInvariant();
            if (lowerMime.Contains("mp3") || lowerMime.Contains("mpeg") || lowerMime.Contains("ogg") || lowerMime.Contains("opus") || lowerMime.Contains("aac") || lowerMime.Contains("flac"))
            {
                return true;
            }
        }

        if (bytes == null || bytes.Length < 4) return false;

        // Magic bytes check: ID3 tag or MP3 sync word (0xFF 0xFB / 0xFF 0xF3 / 0xFF 0xF2)
        if (bytes[0] == 0x49 && bytes[1] == 0x44 && bytes[2] == 0x33) return true; // ID3
        if (bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0) return true; // MP3 frame header
        if (bytes[0] == 0x4F && bytes[1] == 0x67 && bytes[2] == 0x67 && bytes[3] == 0x53) return true; // OggS
        if (bytes[0] == 0x66 && bytes[1] == 0x4C && bytes[2] == 0x61 && bytes[3] == 0x43) return true; // fLaC

        return false;
    }

    private static int? ExtractSampleRateFromMimeType(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType)) return null;
        var match = System.Text.RegularExpressions.Regex.Match(mimeType, @"rate=(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var parsed) && parsed > 0)
        {
            return parsed;
        }
        return null;
    }

    private static bool IsWav(byte[] bytes) => bytes.Length >= 12 &&
        Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" &&
        Encoding.ASCII.GetString(bytes, 8, 4) == "WAVE";

    private static bool TryReadWavMetadata(byte[] wavBytes, out WavMetadata metadata, out string error)
    {
        metadata = default;
        error = "Voice provider returned an invalid WAV payload.";
        if (!IsWav(wavBytes)) return false;

        int offset = 12;
        int channels = 0, sampleRate = 0, bitDepth = 0, byteRate = 0, dataLength = -1;
        while (offset + 8 <= wavBytes.Length)
        {
            var chunkId = Encoding.ASCII.GetString(wavBytes, offset, 4);
            var chunkSize = BitConverter.ToInt32(wavBytes, offset + 4);
            offset += 8;
            if (chunkSize < 0 || offset + chunkSize > wavBytes.Length) return false;
            if (chunkId == "fmt ")
            {
                if (chunkSize < 16) return false;
                var format = BitConverter.ToInt16(wavBytes, offset);
                channels = BitConverter.ToInt16(wavBytes, offset + 2);
                sampleRate = BitConverter.ToInt32(wavBytes, offset + 4);
                byteRate = BitConverter.ToInt32(wavBytes, offset + 8);
                bitDepth = BitConverter.ToInt16(wavBytes, offset + 14);
                if (format != 1 || channels != 1 || bitDepth != 16 || sampleRate <= 0 || byteRate <= 0)
                {
                    error = "Voice provider WAV must be PCM signed 16-bit mono with a valid sample rate.";
                    return false;
                }
            }
            else if (chunkId == "data")
            {
                dataLength = chunkSize;
            }
            offset += chunkSize + (chunkSize % 2);
        }
        if (channels == 0 || dataLength < 0) return false;
        metadata = new WavMetadata(sampleRate, channels, bitDepth, byteRate, dataLength);
        return true;
    }

    private readonly record struct WavMetadata(int SampleRateHz, int Channels, int BitDepth, int ByteRate, int DataLength);

    private static byte[] WrapPcmInWav(byte[] pcmBytes, int sampleRate)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + pcmBytes.Length);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));

        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write((short)1); // Mono
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);

        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(pcmBytes.Length);
        writer.Write(pcmBytes);

        return ms.ToArray();
    }

    private string ResolveVoiceName(VoiceProfile profile, string? configuredLocale)
    {
        // Locale and editorial profile shape prompting; provider voice selection stays configuration-driven.
        return _defaultVoice;
    }

    private static string ResolveAlternateVoiceName(string primaryVoice)
    {
        // Alternate voice for candidate 2 variant
        if (primaryVoice.Equals("Kore", StringComparison.OrdinalIgnoreCase)) return "Aoede";
        if (primaryVoice.Equals("Aoede", StringComparison.OrdinalIgnoreCase)) return "Kore";
        if (primaryVoice.Equals("Charon", StringComparison.OrdinalIgnoreCase)) return "Puck";
        return "Aoede";
    }

    private static string SanitizeForLog(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        if (raw.Length > 300) return raw.Substring(0, 300) + "...";
        return raw;
    }
}
