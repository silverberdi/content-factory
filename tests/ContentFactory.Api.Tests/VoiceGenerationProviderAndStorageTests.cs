using System.Text;
using ContentFactory.Api.Infrastructure.Storage;
using ContentFactory.Api.Modules.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContentFactory.Api.Tests;

public class VoiceGenerationProviderAndStorageTests
{
    [Fact]
    public void GeminiTtsProvider_UsesRequestedDefaultAndPreservesConfiguredOverrides()
    {
        var defaultConfig = new ConfigurationBuilder().Build();
        var defaultProvider = new GeminiTtsProvider(new TestHttpClientFactory(), defaultConfig, NullLogger<GeminiTtsProvider>.Instance);
        Assert.Equal("gemini-3.8-flash-lite-tts", defaultProvider.ModelOrWorkflowIdentifier);

        var environmentOverride = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GEMINI_TTS_MODEL"] = "configured-tts-model"
        }).Build();
        var environmentProvider = new GeminiTtsProvider(new TestHttpClientFactory(), environmentOverride, NullLogger<GeminiTtsProvider>.Instance);
        Assert.Equal("configured-tts-model", environmentProvider.ModelOrWorkflowIdentifier);

        var sectionOverride = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Gemini:TtsModel"] = "section-tts-model"
        }).Build();
        var sectionProvider = new GeminiTtsProvider(new TestHttpClientFactory(), sectionOverride, NullLogger<GeminiTtsProvider>.Instance);
        Assert.Equal("section-tts-model", sectionProvider.ModelOrWorkflowIdentifier);
    }

    [Fact]
    public async Task MockVoiceProvider_GeneratesValidPcmWavHeader_AndMetadata()
    {
        var provider = new MockVoiceGenerationProvider(NullLogger<MockVoiceGenerationProvider>.Instance);

        var profile = VoiceProfiles.IaSimpleEsDefault;
        var request = new VoiceGenerationRequest(
            JobId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid().ToString("N"),
            ContentItemId: Guid.NewGuid(),
            ChannelId: Guid.NewGuid(),
            ScriptId: Guid.NewGuid(),
            ScriptVersionId: Guid.NewGuid(),
            StoryboardId: Guid.NewGuid(),
            StoryboardVersionId: Guid.NewGuid(),
            AssetRequirementId: Guid.NewGuid(),
            Language: "es",
            ConfiguredLocale: "es-ES",
            NarrationText: "La inteligencia artificial nos ayuda a automatizar procesos repetitivos.",
            VoiceProfile: profile,
            PronunciationGuidance: null,
            CandidateCount: 2
        );

        var result = await provider.GenerateVoiceAssetAsync(request);

        Assert.True(result.Success);
        Assert.Equal(2, result.Outputs.Count);
        Assert.Equal("Mock", result.Provider);
        Assert.True(result.IsSimulated);

        var out1 = result.Outputs[0];
        Assert.Equal("audio/wav", out1.ContentType);
        Assert.Equal("wav", out1.FileExtension);
        Assert.Equal(24000, out1.SampleRateHz);
        Assert.Equal(1, out1.Channels);
        Assert.Equal(16, out1.BitDepth);
        Assert.True(out1.DurationSeconds > 0);
        Assert.NotEmpty(out1.AudioBytes);

        // Verify WAV header
        var headerRiff = Encoding.ASCII.GetString(out1.AudioBytes, 0, 4);
        var headerWave = Encoding.ASCII.GetString(out1.AudioBytes, 8, 4);
        var headerFmt = Encoding.ASCII.GetString(out1.AudioBytes, 12, 4);
        var headerData = Encoding.ASCII.GetString(out1.AudioBytes, 36, 4);

        Assert.Equal("RIFF", headerRiff);
        Assert.Equal("WAVE", headerWave);
        Assert.Equal("fmt ", headerFmt);
        Assert.Equal("data", headerData);

        // Sample rate in WAV header (bytes 24-27)
        var sampleRateInHeader = BitConverter.ToInt32(out1.AudioBytes, 24);
        Assert.Equal(24000, sampleRateInHeader);
    }

    [Fact]
    public async Task MockVoiceProvider_SimulatesRetryableFailure()
    {
        var provider = new MockVoiceGenerationProvider(NullLogger<MockVoiceGenerationProvider>.Instance);

        var profile = VoiceProfiles.IaSimpleEsDefault;
        var request = new VoiceGenerationRequest(
            JobId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid().ToString("N"),
            ContentItemId: Guid.NewGuid(),
            ChannelId: Guid.NewGuid(),
            ScriptId: Guid.NewGuid(),
            ScriptVersionId: Guid.NewGuid(),
            StoryboardId: Guid.NewGuid(),
            StoryboardVersionId: Guid.NewGuid(),
            AssetRequirementId: Guid.NewGuid(),
            Language: "es",
            ConfiguredLocale: "es-ES",
            NarrationText: "Testing [mock:retryable-failure] token behavior.",
            VoiceProfile: profile,
            PronunciationGuidance: null,
            CandidateCount: 1
        );

        var result = await provider.GenerateVoiceAssetAsync(request);

        Assert.False(result.Success);
        Assert.True(result.IsRetryable);
        Assert.Equal("PROVIDER_TRANSIENT_503", result.ErrorCode);
    }

    [Fact]
    public async Task MockVoiceProvider_SimulatesActionRequiredFailure()
    {
        var provider = new MockVoiceGenerationProvider(NullLogger<MockVoiceGenerationProvider>.Instance);

        var profile = VoiceProfiles.IaSimpleEsDefault;
        var request = new VoiceGenerationRequest(
            JobId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid().ToString("N"),
            ContentItemId: Guid.NewGuid(),
            ChannelId: Guid.NewGuid(),
            ScriptId: Guid.NewGuid(),
            ScriptVersionId: Guid.NewGuid(),
            StoryboardId: Guid.NewGuid(),
            StoryboardVersionId: Guid.NewGuid(),
            AssetRequirementId: Guid.NewGuid(),
            Language: "es",
            ConfiguredLocale: "es-ES",
            NarrationText: "Testing [mock:action-required-failure] token behavior.",
            VoiceProfile: profile,
            PronunciationGuidance: null,
            CandidateCount: 1
        );

        var result = await provider.GenerateVoiceAssetAsync(request);

        Assert.False(result.Success);
        Assert.False(result.IsRetryable);
        Assert.Equal("INVALID_VOICE_CONFIGURATION", result.ErrorCode);
    }

    [Fact]
    public async Task GeminiTtsProvider_HandlesMissingApiKeyGracefully()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GEMINI_API_KEY"] = ""
            })
            .Build();

        var provider = new GeminiTtsProvider(new TestHttpClientFactory(), config, NullLogger<GeminiTtsProvider>.Instance);

        var profile = VoiceProfiles.IaSimpleEsDefault;
        var request = new VoiceGenerationRequest(
            JobId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid().ToString("N"),
            ContentItemId: Guid.NewGuid(),
            ChannelId: Guid.NewGuid(),
            ScriptId: Guid.NewGuid(),
            ScriptVersionId: Guid.NewGuid(),
            StoryboardId: Guid.NewGuid(),
            StoryboardVersionId: Guid.NewGuid(),
            AssetRequirementId: Guid.NewGuid(),
            Language: "es",
            ConfiguredLocale: "es-ES",
            NarrationText: "Testing Gemini TTS provider without API key.",
            VoiceProfile: profile,
            PronunciationGuidance: null,
            CandidateCount: 1
        );

        var result = await provider.GenerateVoiceAssetAsync(request);

        Assert.False(result.Success);
        Assert.False(result.IsRetryable);
        Assert.Equal("MISSING_API_KEY", result.ErrorCode);
        Assert.Contains("Gemini API key is not configured", result.SanitizedErrorMessage);
    }

    [Fact]
    public async Task GeminiTtsProvider_RejectsCompressedAudioPayloads_WithUnsupportedEncodingErrorCode()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GEMINI_API_KEY"] = "VALID_TEST_KEY"
            })
            .Build();

        // Simulate Gemini returning an MP3 payload (ID3 magic bytes) instead of PCM/WAV
        var mp3Base64 = Convert.ToBase64String(new byte[] { 0x49, 0x44, 0x33, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        var mockResponseJson = $"{{\"candidates\":[{{\"content\":{{\"parts\":[{{\"inlineData\":{{\"mimeType\":\"audio/mp3\",\"data\":\"{mp3Base64}\"}}}}]}}}}]}}";

        var clientFactory = new TestHttpClientFactory(req => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(mockResponseJson, Encoding.UTF8, "application/json")
        });

        var provider = new GeminiTtsProvider(clientFactory, config, NullLogger<GeminiTtsProvider>.Instance);

        var profile = VoiceProfiles.IaSimpleEsDefault;
        var request = new VoiceGenerationRequest(
            JobId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid().ToString("N"),
            ContentItemId: Guid.NewGuid(),
            ChannelId: Guid.NewGuid(),
            ScriptId: Guid.NewGuid(),
            ScriptVersionId: Guid.NewGuid(),
            StoryboardId: Guid.NewGuid(),
            StoryboardVersionId: Guid.NewGuid(),
            AssetRequirementId: Guid.NewGuid(),
            Language: "es",
            ConfiguredLocale: "es-ES",
            NarrationText: "Test compressed audio rejection",
            VoiceProfile: profile,
            PronunciationGuidance: null,
            CandidateCount: 1
        );

        var result = await provider.GenerateVoiceAssetAsync(request);

        Assert.False(result.Success);
        Assert.Equal("PROVIDER_UNSUPPORTED_ENCODING", result.ErrorCode);
    }

    [Fact]
    public async Task GeminiTtsProvider_UsesHeaderAuth_AndRequiresRateForRawPcm()
    {
        var observedHeader = false;
        var rawPcm = Convert.ToBase64String(new byte[] { 0, 0, 1, 0 });
        var body = $"{{\"candidates\":[{{\"content\":{{\"parts\":[{{\"inlineData\":{{\"mimeType\":\"audio/L16\",\"data\":\"{rawPcm}\"}}}}]}}}}]}}";
        var provider = CreateGeminiProvider(request =>
        {
            observedHeader = request.Headers.Contains("x-goog-api-key") && !request.RequestUri!.Query.Contains("key=");
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        });

        var result = await provider.GenerateVoiceAssetAsync(CreateRequest());
        Assert.True(observedHeader);
        Assert.False(result.Success);
        Assert.Equal("PROVIDER_MISSING_SAMPLE_RATE", result.ErrorCode);
    }

    [Fact]
    public async Task GeminiTtsProvider_PreservesExplicitRawPcmRate()
    {
        var rawPcm = Convert.ToBase64String(new byte[] { 0, 0, 1, 0, 2, 0, 3, 0 });
        var body = $"{{\"candidates\":[{{\"content\":{{\"parts\":[{{\"inlineData\":{{\"mimeType\":\"audio/L16;rate=44100\",\"data\":\"{rawPcm}\"}}}}]}}}}]}}";
        var provider = CreateGeminiProvider(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });

        var result = await provider.GenerateVoiceAssetAsync(CreateRequest());
        Assert.True(result.Success);
        Assert.Equal(44100, result.Outputs.Single().SampleRateHz);
    }

    [Fact]
    public async Task GeminiTtsProvider_ParsesWavWithNonTrivialChunkLayout()
    {
        var wav = BuildWavWithJunkChunk(sampleRate: 48000, pcm: new byte[] { 0, 0, 1, 0, 2, 0, 3, 0 });
        var body = $"{{\"candidates\":[{{\"content\":{{\"parts\":[{{\"inlineData\":{{\"mimeType\":\"audio/wav\",\"data\":\"{Convert.ToBase64String(wav)}\"}}}}]}}}}]}}";
        var provider = CreateGeminiProvider(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });

        var result = await provider.GenerateVoiceAssetAsync(CreateRequest());
        Assert.True(result.Success);
        Assert.Equal(48000, result.Outputs.Single().SampleRateHz);
        Assert.Equal(wav, result.Outputs.Single().AudioBytes);
    }

    private static GeminiTtsProvider CreateGeminiProvider(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["GEMINI_API_KEY"] = "VALID_TEST_KEY" }).Build();
        return new GeminiTtsProvider(new TestHttpClientFactory(handler), config, NullLogger<GeminiTtsProvider>.Instance);
    }

    private static VoiceGenerationRequest CreateRequest() => new(
        Guid.NewGuid(), Guid.NewGuid().ToString("N"), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        "es", "es-ES", "Prueba de audio.", VoiceProfiles.IaSimpleEsDefault, null, 1);

    private static byte[] BuildWavWithJunkChunk(int sampleRate, byte[] pcm)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(0); // patched once all chunks are written
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("JUNK")); writer.Write(3); writer.Write(new byte[] { 1, 2, 3 }); writer.Write((byte)0);
        writer.Write(Encoding.ASCII.GetBytes("fmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(sampleRate); writer.Write(sampleRate * 2); writer.Write((short)2); writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(pcm.Length); writer.Write(pcm);
        writer.Flush();
        stream.Position = 4; writer.Write((int)stream.Length - 8); writer.Flush();
        return stream.ToArray();
    }

    [Fact]
    public void StorageService_GeneratesCorrectAudioObjectKey()
    {
        var config = new ConfigurationBuilder().Build();
        var storage = new MinioStorageService(config, new TestHttpClientFactory(), NullLogger<MinioStorageService>.Instance);

        var channelId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var contentItemId = Guid.Parse("00000000-0000-0000-0000-000000000020");
        var storyboardVersionId = Guid.Parse("00000000-0000-0000-0000-000000000030");
        var assetRequirementId = Guid.Parse("00000000-0000-0000-0000-000000000040");
        var generatedAssetId = Guid.Parse("00000000-0000-0000-0000-000000000050");

        var key = storage.GenerateAudioObjectKey(
            environment: "development",
            channelId: channelId,
            contentItemId: contentItemId,
            storyboardVersionId: storyboardVersionId,
            assetRequirementId: assetRequirementId,
            generatedAssetId: generatedAssetId,
            fileExtension: "wav"
        );

        var expected = $"content-factory/development/channels/{channelId}/content/{contentItemId}/storyboard/{storyboardVersionId}/audio/voice/{assetRequirementId}/{generatedAssetId}.wav";
        Assert.Equal(expected, key);
    }

    [Fact]
    public async Task LiveGeminiTts_SmokeTest_ExecutesFullDomainFlow()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_LIVE_GEMINI_TTS_TESTS"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Program.LoadEnvFile();

        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "CHANGE_ME")
        {
            // Try loading directly from repo root .env.development if test runner CWD is nested
            var rootEnvDev = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", ".env.development");
            if (File.Exists(rootEnvDev))
            {
                foreach (var line in File.ReadAllLines(rootEnvDev))
                {
                    if (line.StartsWith("GEMINI_API_KEY="))
                    {
                        apiKey = line.Split('=', 2)[1].Trim('"', '\'').Trim();
                        Environment.SetEnvironmentVariable("GEMINI_API_KEY", apiKey);
                        break;
                    }
                }
            }
        }

        Assert.False(string.IsNullOrWhiteSpace(apiKey) || apiKey == "CHANGE_ME", "GEMINI_API_KEY must be configured in .env.development for live smoke test execution.");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GEMINI_API_KEY"] = apiKey,
                ["GEMINI_TTS_MODEL"] = "gemini-3.8-flash-lite-tts",
                ["GEMINI_VOICE_NAME"] = "Kore",
                ["VOICE_PROVIDER"] = "Gemini"
            })
            .Build();

        var provider = new GeminiTtsProvider(new TestHttpClientFactory(), config, NullLogger<GeminiTtsProvider>.Instance);

        var profile = VoiceProfiles.IaSimpleEsDefault;
        var request = new VoiceGenerationRequest(
            JobId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid().ToString("N"),
            ContentItemId: Guid.NewGuid(),
            ChannelId: Guid.NewGuid(),
            ScriptId: Guid.NewGuid(),
            ScriptVersionId: Guid.NewGuid(),
            StoryboardId: Guid.NewGuid(),
            StoryboardVersionId: Guid.NewGuid(),
            AssetRequirementId: Guid.NewGuid(),
            Language: "es",
            ConfiguredLocale: "es-ES",
            NarrationText: "Bienvenidos a Content Factory. Este es un test de voz en vivo utilizando inteligencia artificial.",
            VoiceProfile: profile,
            PronunciationGuidance: null,
            CandidateCount: 1
        );

        var result = await provider.GenerateVoiceAssetAsync(request);

        Assert.True(result.Success, $"Gemini TTS execution failed: {result.ErrorCode} - {result.SanitizedErrorMessage}");
        Assert.NotEmpty(result.Outputs);

        var output = result.Outputs[0];

        // Format and PCM checks
        Assert.Equal("audio/wav", output.ContentType);
        Assert.Equal("wav", output.FileExtension);
        Assert.Equal(1, output.Channels);
        Assert.Equal(16, output.BitDepth);
        Assert.True(output.SampleRateHz > 0);
        Assert.True(output.DurationSeconds > 0);
        Assert.NotEmpty(output.AudioBytes);

        // Header validation: RIFF + WAVE + fmt 
        Assert.True(output.AudioBytes.Length > 44);
        Assert.Equal("RIFF", Encoding.ASCII.GetString(output.AudioBytes, 0, 4));
        Assert.Equal("WAVE", Encoding.ASCII.GetString(output.AudioBytes, 8, 4));
        Assert.Equal("fmt ", Encoding.ASCII.GetString(output.AudioBytes, 12, 4));

        // Model & parameters snapshot checks
        Assert.Contains("gemini-3.8-flash-lite-tts", output.ProviderModel);
        Assert.Contains("Kore", output.ProviderModel);
        Assert.Contains("gemini-3.8-flash-lite-tts", output.GenerationParametersSnapshot);

        Console.WriteLine($"LIVE_GEMINI_TTS_METADATA model={output.ProviderModel}; contentType={output.ContentType}; sampleRateHz={output.SampleRateHz}; channels={output.Channels}; bitDepth={output.BitDepth}; durationSeconds={output.DurationSeconds}");

        // Security check: no secrets present
        Assert.DoesNotContain(apiKey, output.ProviderModel);
        Assert.DoesNotContain(apiKey, output.GenerationParametersSnapshot);
    }
}
