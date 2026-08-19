using System.Text;
using ContentFactory.Api.Infrastructure.Storage;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Content;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContentFactory.Api.Tests;

public class VisualGenerationProviderAndStorageTests
{
    [Fact]
    public async Task MinioStorageService_GeneratesDeterministicKey_AndUploadsCorrectly()
    {
        var config = new ConfigurationBuilder().Build();
        var httpClientFactory = new TestHttpClientFactory();
        var storage = new MinioStorageService(config, httpClientFactory, NullLogger<MinioStorageService>.Instance);

        var channelId = Guid.NewGuid();
        var contentItemId = Guid.NewGuid();
        var storyboardVersionId = Guid.NewGuid();
        var reqId = Guid.NewGuid();
        var assetId = Guid.NewGuid();

        var key = storage.GenerateObjectKey("development", channelId, contentItemId, storyboardVersionId, reqId, assetId, "png");
        Assert.Equal($"content-factory/development/channels/{channelId}/content/{contentItemId}/storyboard/{storyboardVersionId}/visual/{reqId}/{assetId}.png", key);

        var testBytes = Encoding.UTF8.GetBytes("Test image content binary stream");
        using var stream = new MemoryStream(testBytes);

        var uploadResult = await storage.UploadAsync(key, stream, "image/png");
        Assert.Equal(key, uploadResult.ObjectKey);
        Assert.Equal("image/png", uploadResult.ContentType);
        Assert.Equal(testBytes.Length, uploadResult.FileSizeBytes);
        Assert.False(string.IsNullOrWhiteSpace(uploadResult.ChecksumSha256));

        var exists = await storage.ExistsAsync(key);
        Assert.True(exists);

        var downloadResult = await storage.DownloadAsync(key);
        Assert.True(downloadResult.Success);
        Assert.NotNull(downloadResult.Stream);
        Assert.Equal(testBytes.Length, downloadResult.FileSizeBytes);

        using var memoryStream = new MemoryStream();
        await downloadResult.Stream.CopyToAsync(memoryStream);
        Assert.Equal(testBytes, memoryStream.ToArray());
    }

    [Fact]
    public async Task MockVisualGenerationProvider_GeneratesDeterministicMedia_ForMultipleCandidates()
    {
        var provider = new MockVisualGenerationProvider(NullLogger<MockVisualGenerationProvider>.Instance);
        var request = new VisualGenerationRequest(
            JobId: Guid.NewGuid(),
            CorrelationId: "corr-1",
            ContentItemId: Guid.NewGuid(),
            ChannelId: Guid.NewGuid(),
            StoryboardId: Guid.NewGuid(),
            StoryboardVersionId: Guid.NewGuid(),
            AssetRequirementId: Guid.NewGuid(),
            AssetType: AssetType.AiImage,
            AspectRatio: "9:16",
            TargetWidth: 1080,
            TargetHeight: 1920,
            TargetDurationSeconds: null,
            VisualPrompt: "Close-up of AI chip on futuristic silicon board",
            NegativePrompt: "blurry, lowres",
            StyleIntent: "Clean tech dark mode",
            MotionIntent: "Static",
            CandidateCount: 2
        );

        var result = await provider.GenerateVisualAssetAsync(request);

        Assert.True(result.Success);
        Assert.Equal(2, result.Outputs.Count);
        Assert.False(result.IsRetryable);
        Assert.Null(result.ErrorCode);

        var cand1 = result.Outputs[0];
        Assert.Equal(1, cand1.VariantIndex);
        Assert.Equal(1080, cand1.Width);
        Assert.Equal(1920, cand1.Height);
        Assert.Equal("image/svg+xml", cand1.ContentType);
        Assert.True(cand1.MediaBytes.Length > 100);

        var svgString = Encoding.UTF8.GetString(cand1.MediaBytes);
        Assert.Contains("CONTENT FACTORY", svgString);
        Assert.Contains("CANDIDATE #1", svgString);
        Assert.Contains("9:16 VERTICAL", svgString);

        var cand2 = result.Outputs[1];
        Assert.Equal(2, cand2.VariantIndex);
    }

    [Fact]
    public async Task MockVisualGenerationProvider_SimulatesTransientFailure_OnTriggerToken()
    {
        var provider = new MockVisualGenerationProvider(NullLogger<MockVisualGenerationProvider>.Instance);
        var request = new VisualGenerationRequest(
            JobId: Guid.NewGuid(),
            CorrelationId: "corr-2",
            ContentItemId: Guid.NewGuid(),
            ChannelId: Guid.NewGuid(),
            StoryboardId: Guid.NewGuid(),
            StoryboardVersionId: Guid.NewGuid(),
            AssetRequirementId: Guid.NewGuid(),
            AssetType: AssetType.AiImage,
            AspectRatio: "9:16",
            TargetWidth: 1080,
            TargetHeight: 1920,
            TargetDurationSeconds: null,
            VisualPrompt: "Cyberpunk alleyway [mock:retryable-failure]",
            NegativePrompt: "",
            StyleIntent: "",
            MotionIntent: "",
            CandidateCount: 1
        );

        var result = await provider.GenerateVisualAssetAsync(request);

        Assert.False(result.Success);
        Assert.True(result.IsRetryable);
        Assert.Equal("PROVIDER_TRANSIENT_503", result.ErrorCode);
        Assert.Empty(result.Outputs);
    }

    [Fact]
    public async Task MockVisualGenerationProvider_SimulatesActionRequiredFailure_OnTriggerToken()
    {
        var provider = new MockVisualGenerationProvider(NullLogger<MockVisualGenerationProvider>.Instance);
        var request = new VisualGenerationRequest(
            JobId: Guid.NewGuid(),
            CorrelationId: "corr-3",
            ContentItemId: Guid.NewGuid(),
            ChannelId: Guid.NewGuid(),
            StoryboardId: Guid.NewGuid(),
            StoryboardVersionId: Guid.NewGuid(),
            AssetRequirementId: Guid.NewGuid(),
            AssetType: AssetType.AiImage,
            AspectRatio: "9:16",
            TargetWidth: 1080,
            TargetHeight: 1920,
            TargetDurationSeconds: null,
            VisualPrompt: "Futuristic city [mock:action-required-failure]",
            NegativePrompt: "",
            StyleIntent: "",
            MotionIntent: "",
            CandidateCount: 1
        );

        var result = await provider.GenerateVisualAssetAsync(request);

        Assert.False(result.Success);
        Assert.False(result.IsRetryable);
        Assert.Equal("INVALID_WORKFLOW_CONFIGURATION", result.ErrorCode);
        Assert.Empty(result.Outputs);
    }

    private class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name = "") => new();
    }
}
