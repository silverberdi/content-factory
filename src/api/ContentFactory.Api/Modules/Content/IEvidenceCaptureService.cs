using System.Security.Cryptography;
using System.Text;
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
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "ContentFactoryEvidenceCollector/1.0 (+https://silverman.pro)");

            var response = await client.GetAsync(evidence.OriginUrl!, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var html = await response.Content.ReadAsStringAsync(cancellationToken);
                var extracted = ExtractReadableText(html);

                evidence.RawContent = html.Length > 20000 ? html[..20000] : html;
                evidence.ExtractedText = string.IsNullOrWhiteSpace(extracted) ? fallbackText : extracted;
                evidence.ContentHash = ComputeSha256Hash(evidence.ExtractedText ?? evidence.RawContent ?? evidence.Title);
                evidence.Status = EvidenceStatus.Captured;
                evidence.ErrorMessage = null;
            }
            else
            {
                // Truthful HTTP failure recording
                evidence.Status = EvidenceStatus.CaptureFailed;
                evidence.ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                evidence.ExtractedText = fallbackText;
                evidence.ContentHash = ComputeSha256Hash(fallbackText ?? evidence.Title);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to capture remote URL evidence from {Url}", evidence.OriginUrl);
            evidence.Status = EvidenceStatus.CaptureFailed;
            evidence.ErrorMessage = $"Capture exception: {ex.Message}";
            evidence.ExtractedText = fallbackText;
            evidence.ContentHash = ComputeSha256Hash(fallbackText ?? evidence.Title);
        }
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

    [GeneratedRegex(@"<script[^>]*>[\s\S]*?</script>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptRegex();

    [GeneratedRegex(@"<style[^>]*>[\s\S]*?</style>", RegexOptions.IgnoreCase)]
    private static partial Regex StyleRegex();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
