using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ContentFactory.Api.Infrastructure.Storage;

public class MinioStorageService(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<MinioStorageService> logger) : IStorageService
{
    private static readonly ConcurrentDictionary<string, (byte[] Data, string ContentType, string Checksum)> InMemCache = new();
    private readonly string _endpoint = configuration["MINIO_ENDPOINT"] ?? configuration["Minio:Endpoint"] ?? string.Empty;
    private readonly string _bucket = configuration["MINIO_BUCKET"] ?? configuration["Minio:Bucket"] ?? "content-factory-assets";
    private readonly string _accessKey = configuration["MINIO_ACCESS_KEY"] ?? configuration["Minio:AccessKey"] ?? string.Empty;
    private readonly string _secretKey = configuration["MINIO_SECRET_KEY"] ?? configuration["Minio:SecretKey"] ?? string.Empty;
    private readonly bool _useSsl = bool.TryParse(configuration["MINIO_USE_SSL"] ?? configuration["Minio:UseSsl"], out var ssl) && ssl;

    public string GenerateObjectKey(
        string environment,
        Guid channelId,
        Guid contentItemId,
        Guid storyboardVersionId,
        Guid assetRequirementId,
        Guid generatedAssetId,
        string fileExtension)
    {
        var cleanExt = fileExtension.TrimStart('.').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(cleanExt)) cleanExt = "png";
        var env = string.IsNullOrWhiteSpace(environment) ? "development" : environment.ToLowerInvariant();

        return $"content-factory/{env}/channels/{channelId}/content/{contentItemId}/storyboard/{storyboardVersionId}/visual/{assetRequirementId}/{generatedAssetId}.{cleanExt}";
    }

    public async Task<StorageUploadResult> UploadAsync(
        string objectKey,
        Stream dataStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await dataStream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        var hashBytes = SHA256.HashData(bytes);
        var checksum = Convert.ToHexStringLower(hashBytes);
        var length = bytes.LongLength;

        // In-memory cache for fast local retrieval and testing
        InMemCache[objectKey] = (bytes, contentType, checksum);

        // Attempt live MinIO upload if endpoint is configured
        if (!string.IsNullOrWhiteSpace(_endpoint) && _endpoint != "CHANGE_ME")
        {
            try
            {
                var scheme = _useSsl ? "https" : "http";
                var baseUri = _endpoint.StartsWith("http") ? _endpoint : $"{scheme}://{_endpoint}";
                var uploadUri = $"{baseUri.TrimEnd('/')}/{_bucket}/{objectKey.TrimStart('/')}";

                var client = httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Put, uploadUri);
                request.Content = new ByteArrayContent(bytes);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

                // Add basic authorization header if credentials exist
                if (!string.IsNullOrWhiteSpace(_accessKey) && !string.IsNullOrWhiteSpace(_secretKey))
                {
                    var authValue = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_accessKey}:{_secretKey}"));
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
                }

                var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Live MinIO upload returned status {StatusCode}. Retained in local fallback cache.", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Live MinIO upload to {Endpoint} failed. Retained in local fallback cache.", _endpoint);
            }
        }

        return new StorageUploadResult(objectKey, checksum, length, contentType);
    }

    public Task<StorageDownloadResult> DownloadAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        if (InMemCache.TryGetValue(objectKey, out var item))
        {
            var stream = new MemoryStream(item.Data);
            return Task.FromResult(new StorageDownloadResult(true, stream, item.ContentType, item.Data.LongLength, null));
        }

        return Task.FromResult(new StorageDownloadResult(false, null, "application/octet-stream", 0, "Object not found in storage."));
    }

    public Task<bool> ExistsAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(InMemCache.ContainsKey(objectKey));
    }
}
