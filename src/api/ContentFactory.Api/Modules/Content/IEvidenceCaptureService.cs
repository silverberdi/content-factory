using System.Security.Cryptography;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ContentFactory.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Modules.Content;

public record CapturedEvidenceResult(
    bool Success,
    ContentItemEvidence Evidence,
    string? ErrorMessage
);

public interface IEvidenceCaptureService
{
    Task<CapturedEvidenceResult> CaptureEvidenceAsync(
        Guid contentItemId,
        Guid? candidateId,
        string? url,
        string title,
        string? rawText,
        string role,
        string? notes,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<CapturedEvidenceResult> RetryCaptureAsync(
        Guid contentItemId,
        Guid evidenceId,
        string actorEmail,
        CancellationToken cancellationToken = default);
}

public partial class EvidenceCaptureService(
    AppDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    ILogger<EvidenceCaptureService> logger) : IEvidenceCaptureService
{
    public async Task<CapturedEvidenceResult> CaptureEvidenceAsync(
        Guid contentItemId,
        Guid? candidateId,
        string? url,
        string title,
        string? rawText,
        string role,
        string? notes,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var evidence = new ContentItemEvidence
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            DiscoveryCandidateId = candidateId,
            OriginUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim(),
            Title = string.IsNullOrWhiteSpace(title) ? "Captured Evidence" : title.Trim(),
            Role = EvidenceRole.All.Contains(role) ? role : EvidenceRole.PrimaryLead,
            Notes = notes,
            CapturedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = actorEmail
        };

        if (!string.IsNullOrWhiteSpace(evidence.OriginUrl))
        {
            await ProcessUrlCaptureAsync(evidence, rawText, cancellationToken);
        }
        else
        {
            ProcessTextCapture(evidence, rawText);
        }

        dbContext.ContentItemEvidences.Add(evidence);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CapturedEvidenceResult(
            evidence.Status == EvidenceStatus.Captured,
            evidence,
            evidence.ErrorMessage);
    }

    public async Task<CapturedEvidenceResult> RetryCaptureAsync(
        Guid contentItemId,
        Guid evidenceId,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var evidence = await dbContext.ContentItemEvidences
            .FirstOrDefaultAsync(e => e.Id == evidenceId && e.ContentItemId == contentItemId, cancellationToken);

        if (evidence == null)
        {
            return new CapturedEvidenceResult(false, null!, "Evidence item not found");
        }

        if (string.IsNullOrWhiteSpace(evidence.OriginUrl))
        {
            return new CapturedEvidenceResult(false, evidence, "Cannot retry capture for a text-only evidence lead.");
        }

        await ProcessUrlCaptureAsync(evidence, evidence.RawContent, cancellationToken);
        evidence.CapturedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CapturedEvidenceResult(
            evidence.Status == EvidenceStatus.Captured,
            evidence,
            evidence.ErrorMessage);
    }

    private async Task ProcessUrlCaptureAsync(ContentItemEvidence evidence, string? fallbackText, CancellationToken cancellationToken)
    {
        try
        {
            var currentUrl = evidence.OriginUrl!;
            for (var redirect = 0; redirect <= 5; redirect++)
            {
                if (!await IsSafePublicUrlAsync(currentUrl, cancellationToken))
                {
                    MarkCaptureFailure(evidence, "URL_DESTINATION_NOT_ALLOWED", fallbackText);
                    return;
                }

                var client = httpClientFactory.CreateClient("evidence-capture");
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Remove("User-Agent");
                client.DefaultRequestHeaders.Add("User-Agent", "ContentFactoryEvidenceCollector/1.0 (+https://silverman.pro)");
                using var response = await client.GetAsync(currentUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location != null)
                {
                    if (redirect == 5)
                    {
                        MarkCaptureFailure(evidence, "REDIRECT_LIMIT_EXCEEDED", fallbackText);
                        return;
                    }
                    currentUrl = new Uri(new Uri(currentUrl), response.Headers.Location).ToString();
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    MarkCaptureFailure(evidence, $"HTTP_{(int)response.StatusCode}", fallbackText);
                    return;
                }

                if (response.Content.Headers.ContentLength is > 2_000_000)
                {
                    MarkCaptureFailure(evidence, "RESPONSE_TOO_LARGE", fallbackText);
                    return;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);
                var html = await reader.ReadToEndAsync(cancellationToken);
                if (html.Length > 2_000_000)
                {
                    MarkCaptureFailure(evidence, "RESPONSE_TOO_LARGE", fallbackText);
                    return;
                }
                var extracted = ExtractReadableText(html);
                var metadata = ExtractMetadata(html);
                evidence.RawContent = html.Length > 20000 ? html[..20000] : html;
                evidence.ExtractedText = string.IsNullOrWhiteSpace(extracted) ? fallbackText : extracted;
                if (!string.IsNullOrWhiteSpace(metadata.Title) && evidence.Title == evidence.OriginUrl)
                    evidence.Title = metadata.Title[..Math.Min(metadata.Title.Length, 512)];
                evidence.Author = metadata.Author;
                evidence.Notes = JsonSerializer.Serialize(new
                {
                    language = metadata.Language,
                    locator = metadata.Locator,
                    textOrigin = "captured"
                });
                evidence.ContentHash = ComputeSha256Hash(evidence.ExtractedText ?? evidence.RawContent ?? evidence.Title);
                evidence.Status = EvidenceStatus.Captured;
                evidence.ErrorMessage = null;
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to capture remote URL evidence from {Url}", evidence.OriginUrl);
            MarkCaptureFailure(evidence, "CAPTURE_EXCEPTION", fallbackText);
        }
    }

    private static void MarkCaptureFailure(ContentItemEvidence evidence, string code, string? fallbackText)
    {
        evidence.Status = EvidenceStatus.CaptureFailed;
        evidence.ErrorMessage = code;
        evidence.ExtractedText = fallbackText;
        evidence.ContentHash = ComputeSha256Hash(fallbackText ?? evidence.Title);
    }

    private static async Task<bool> IsSafePublicUrlAsync(string value, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || uri.IsLoopback)
            return false;
        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)) return false;
        if (IPAddress.TryParse(uri.Host, out var parsed)) return IsPublic(parsed);
        IPAddress[] addresses;
        try { addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken); }
        catch { return false; }
        return addresses.Length > 0 && addresses.All(IsPublic);
    }

    private static bool IsPublic(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any)) return false;
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return !(b[0] == 10 || b[0] == 127 || (b[0] == 169 && b[1] == 254) ||
                     (b[0] == 172 && b[1] is >= 16 and <= 31) || (b[0] == 192 && b[1] == 168));
        }
        return !ip.IsIPv6LinkLocal && !ip.IsIPv6SiteLocal && !ip.IsIPv6Multicast;
    }

    private static void ProcessTextCapture(ContentItemEvidence evidence, string? text)
    {
        var content = string.IsNullOrWhiteSpace(text) ? evidence.Title : text.Trim();
        evidence.RawContent = content;
        evidence.ExtractedText = content;
        evidence.ContentHash = ComputeSha256Hash(content);
        evidence.Status = EvidenceStatus.Captured;
        evidence.ErrorMessage = null;
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private static string ExtractReadableText(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        // Remove script and style blocks
        var noScripts = ScriptRegex().Replace(html, " ");
        var noStyles = StyleRegex().Replace(noScripts, " ");

        // Strip HTML tags
        var plainText = HtmlTagRegex().Replace(noStyles, " ");

        // Normalize whitespaces
        var normalized = WhitespaceRegex().Replace(plainText, " ").Trim();
        return normalized;
    }

    private static CaptureMetadata ExtractMetadata(string html)
    {
        var title = Regex.Match(html, @"<title[^>]*>(?<value>[\s\S]*?)</title>", RegexOptions.IgnoreCase).Groups["value"].Value.Trim();
        var language = Regex.Match(html, "<html[^>]+lang=[\\\"'](?<value>[^\\\"']+)", RegexOptions.IgnoreCase).Groups["value"].Value.Trim();
        var author = Regex.Match(html, "<meta[^>]+(?:name|property)=[\\\"'](?:author|article:author)[\\\"'][^>]+content=[\\\"'](?<value>[^\\\"']+)", RegexOptions.IgnoreCase).Groups["value"].Value.Trim();
        return new CaptureMetadata(title, language, author, string.IsNullOrWhiteSpace(title) ? null : "html:title");
    }

    private sealed record CaptureMetadata(string? Title, string? Language, string? Author, string? Locator);

    [GeneratedRegex(@"<script[^>]*>[\s\S]*?</script>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptRegex();

    [GeneratedRegex(@"<style[^>]*>[\s\S]*?</style>", RegexOptions.IgnoreCase)]
    private static partial Regex StyleRegex();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
