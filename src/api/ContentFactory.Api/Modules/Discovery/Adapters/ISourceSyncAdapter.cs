namespace ContentFactory.Api.Modules.Discovery.Adapters;

public record DiscoveredItem(
    string Title,
    string? ExternalUrl,
    string? Summary,
    string? RawContent,
    string? Author,
    DateTime DiscoveredAtUtc,
    string Language
);

public interface ISourceSyncAdapter
{
    bool CanHandle(string sourceType);
    Task<IReadOnlyList<DiscoveredItem>> FetchAsync(DiscoverySource source, CancellationToken cancellationToken = default);
}
