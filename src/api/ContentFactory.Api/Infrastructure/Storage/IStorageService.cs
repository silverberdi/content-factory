namespace ContentFactory.Api.Infrastructure.Storage;

public record StorageUploadResult(
    string ObjectKey,
    string ChecksumSha256,
    long FileSizeBytes,
    string ContentType
);

public record StorageDownloadResult(
    bool Success,
    Stream? Stream,
    string ContentType,
    long FileSizeBytes,
    string? ErrorMessage
);

public interface IStorageService
{
    string GenerateObjectKey(
        string environment,
        Guid channelId,
        Guid contentItemId,
        Guid storyboardVersionId,
        Guid assetRequirementId,
        Guid generatedAssetId,
        string fileExtension);

    Task<StorageUploadResult> UploadAsync(
        string objectKey,
        Stream dataStream,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<StorageDownloadResult> DownloadAsync(
        string objectKey,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string objectKey,
        CancellationToken cancellationToken = default);
}
