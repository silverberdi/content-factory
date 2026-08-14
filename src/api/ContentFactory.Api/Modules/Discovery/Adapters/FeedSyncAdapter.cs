using System.ServiceModel.Syndication;
using System.Xml;

namespace ContentFactory.Api.Modules.Discovery.Adapters;

public class FeedSyncAdapter(HttpClient httpClient) : ISourceSyncAdapter
{
    public bool CanHandle(string sourceType) =>
        string.Equals(sourceType, SourceType.Feed, StringComparison.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<DiscoveredItem>> FetchAsync(DiscoverySource source, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source.OriginUrl))
            return [];

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        using var request = new HttpRequestMessage(HttpMethod.Get, source.OriginUrl);
        request.Headers.UserAgent.ParseAdd("ContentFactory/1.0 (+https://factory.silverman.pro)");

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        var xmlSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            Async = true
        };

        using var reader = XmlReader.Create(stream, xmlSettings);
        var feed = SyndicationFeed.Load(reader);

        if (feed == null)
            return [];

        var items = new List<DiscoveredItem>();
        foreach (var item in feed.Items)
        {
            var title = item.Title?.Text?.Trim() ?? "Untitled";
            
            var link = item.Links.FirstOrDefault(l => l.RelationshipType == "alternate" || string.IsNullOrEmpty(l.RelationshipType))?.Uri?.ToString()
                       ?? item.Links.FirstOrDefault()?.Uri?.ToString();

            var summary = item.Summary?.Text?.Trim();
            var rawContent = (item.Content as TextSyndicationContent)?.Text?.Trim() ?? summary;
            var author = item.Authors.FirstOrDefault()?.Name ?? item.Authors.FirstOrDefault()?.Email;

            var discoveredAt = item.PublishDate.UtcDateTime > DateTime.MinValue && item.PublishDate.UtcDateTime <= DateTime.UtcNow.AddHours(2)
                ? item.PublishDate.UtcDateTime
                : (item.LastUpdatedTime.UtcDateTime > DateTime.MinValue && item.LastUpdatedTime.UtcDateTime <= DateTime.UtcNow.AddHours(2)
                    ? item.LastUpdatedTime.UtcDateTime
                    : DateTime.UtcNow);

            items.Add(new DiscoveredItem(
                Title: title,
                ExternalUrl: link,
                Summary: summary,
                RawContent: rawContent,
                Author: author,
                DiscoveredAtUtc: discoveredAt,
                Language: !string.IsNullOrWhiteSpace(feed.Language) ? feed.Language : source.Language
            ));
        }

        return items;
    }
}
