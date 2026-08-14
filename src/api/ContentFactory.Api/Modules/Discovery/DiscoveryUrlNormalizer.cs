using System.Web;

namespace ContentFactory.Api.Modules.Discovery;

public static class DiscoveryUrlNormalizer
{
    private static readonly HashSet<string> TrackingParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content", "utm_id",
        "fbclid", "gclid", "ref", "source", "mc_cid", "mc_eid", "_hsenc", "_hsmi", "igshid", "si"
    };

    public static string? Normalize(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return null;

        var trimmed = rawUrl.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return trimmed;

        var scheme = uri.Scheme.ToLowerInvariant();
        var host = uri.Host.ToLowerInvariant();
        var port = (uri.IsDefaultPort || (scheme == "http" && uri.Port == 80) || (scheme == "https" && uri.Port == 443))
            ? ""
            : $":{uri.Port}";

        var path = uri.AbsolutePath;
        if (path.Length > 1 && path.EndsWith('/'))
        {
            path = path.TrimEnd('/');
        }

        // Clean query parameters
        var query = uri.Query;
        string? cleanQuery = null;
        if (!string.IsNullOrEmpty(query))
        {
            var parsed = HttpUtility.ParseQueryString(query);
            var kept = new List<string>();
            foreach (string? key in parsed.AllKeys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (!TrackingParameters.Contains(key))
                {
                    var values = parsed.GetValues(key);
                    if (values != null)
                    {
                        foreach (var val in values)
                        {
                            kept.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(val ?? "")}");
                        }
                    }
                }
            }

            if (kept.Count > 0)
            {
                cleanQuery = "?" + string.Join("&", kept);
            }
        }

        return $"{scheme}://{host}{port}{path}{cleanQuery}";
    }
}
