using System.Net;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Channels;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Modules.Content;

public interface IEditorialUrlAnalysisService
{
    Task<EditorialUrlAnalysisDto> AnalyzeAsync(
        EditorialUrlAnalysisRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<EditorialUrlAnalysisDto?> GetAsync(
        Guid analysisId,
        CancellationToken cancellationToken = default);
}

public sealed class EditorialUrlAnalysisService(
    AppDbContext dbContext,
    IEvidenceCaptureService evidenceCaptureService,
    IAiProviderRouter aiProviderRouter,
    ILogger<EditorialUrlAnalysisService> logger) : IEditorialUrlAnalysisService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> IdempotencyLocks = new(StringComparer.Ordinal);

    public async Task<EditorialUrlAnalysisDto> AnalyzeAsync(
        EditorialUrlAnalysisRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var normalizedUrl = NormalizeUrl(request.Url);
        var key = $"{request.ChannelId:N}|{normalizedUrl}";
        var gate = IdempotencyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try { return await AnalyzeCoreAsync(request, normalizedUrl, actorEmail, cancellationToken); }
        finally { gate.Release(); }
    }

    private async Task<EditorialUrlAnalysisDto> AnalyzeCoreAsync(
        EditorialUrlAnalysisRequest request,
        string normalizedUrl,
        string actorEmail,
        CancellationToken cancellationToken)
    {
        var channel = await dbContext.Channels.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId, cancellationToken)
            ?? throw new ArgumentException("Channel not found.");

        // Existing evidence is the durable idempotency key. It works for the
        // current PostgreSQL model without adding a second aggregate or table.
        var existing = await dbContext.ContentItemEvidences
            .Where(e => e.OriginUrl == normalizedUrl)
            .Join(dbContext.ContentItems, e => e.ContentItemId, c => c.Id, (e, c) => new { Evidence = e, Item = c })
            .Where(x => x.Item.ChannelId == request.ChannelId && x.Evidence.Status != EvidenceStatus.Excluded)
            .OrderByDescending(x => x.Evidence.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != null)
        {
            return await MapAsync(existing.Item, existing.Evidence, request.Angle, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var title = string.IsNullOrWhiteSpace(request.Title) ? normalizedUrl : request.Title.Trim();
        var item = new ContentItem
        {
            ChannelId = request.ChannelId,
            Title = title.Length > 512 ? title[..512] : title,
            Slug = BuildSlug(title),
            CreatedByEmail = actorEmail,
            UpdatedByEmail = actorEmail,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        dbContext.ContentItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        CapturedEvidenceResult capture;
        if (!string.IsNullOrWhiteSpace(request.ManualText))
        {
            capture = await evidenceCaptureService.CaptureEvidenceAsync(
                item.Id, null, null, title, request.ManualText, EvidenceRole.PrimaryLead,
                BuildNotes(request.Angle, "manual"), actorEmail, cancellationToken);
            capture.Evidence.OriginUrl = normalizedUrl;
            capture.Evidence.Notes = BuildNotes(request.Angle, "manual");
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            capture = await evidenceCaptureService.CaptureEvidenceAsync(
                item.Id, null, normalizedUrl, title, null, EvidenceRole.PrimaryLead,
                BuildNotes(request.Angle, "captured"), actorEmail, cancellationToken);
        }

        var evidence = capture.Evidence;
        var text = evidence.Status == EvidenceStatus.Captured ? evidence.ExtractedText?.Trim() : null;
        if (!capture.Success)
        {
            return await MapAsync(item, evidence, request.Angle, cancellationToken,
                EditorialUrlAnalysisStatus.Failed, "CAPTURE_FAILED", SanitizeError(capture.ErrorMessage));
        }

        if (string.IsNullOrWhiteSpace(text) || string.Equals(text, title, StringComparison.Ordinal))
        {
            evidence.Status = EvidenceStatus.Captured;
            evidence.Notes = BuildNotes(request.Angle, string.IsNullOrWhiteSpace(request.ManualText) ? "no-text" : "manual");
            await dbContext.SaveChangesAsync(cancellationToken);
            return await MapAsync(item, evidence, request.Angle, cancellationToken,
                EditorialUrlAnalysisStatus.NotAnalyzable, "NO_ANALYZABLE_TEXT", "No accessible text was provided.");
        }

        var result = await aiProviderRouter.BuildTruthSourceAsync(
            new BuildTruthSourceRequest(channel.Name, channel.Language, channel.Niche,
                [new EvidenceSnippetDto(evidence.Id, evidence.Title, evidence.OriginUrl, evidence.Role, text)]),
            new AiRoutingContext(channel.Id, item.Id), cancellationToken);

        if (!result.Success || result.Data == null)
        {
            return await MapAsync(item, evidence, request.Angle, cancellationToken,
                EditorialUrlAnalysisStatus.Failed, result.ErrorCode ?? "AI_ANALYSIS_FAILED",
                SanitizeError(result.ErrorMessage));
        }

        var claims = result.Data.VerifiableClaims
            .Where(c => c.EvidenceId == evidence.Id && !string.IsNullOrWhiteSpace(c.Claim))
            .Select(c => VerifyClaim(c, text))
            .ToList();
        var truthSource = await dbContext.TruthSources.FirstOrDefaultAsync(t => t.ContentItemId == item.Id, cancellationToken);
        if (truthSource == null)
        {
            truthSource = new TruthSource
            {
                ContentItemId = item.Id,
                Status = TruthSourceStatus.Draft,
                Summary = result.Data.Summary,
                KeyIdeasJson = JsonSerializer.Serialize(result.Data.KeyIdeas),
                VerifiableClaimsJson = JsonSerializer.Serialize(claims),
                EvidenceReferencesJson = JsonSerializer.Serialize(new List<Guid> { evidence.Id }),
                RiskNotes = result.Data.RiskNotes,
                DoNotSayConstraintsJson = JsonSerializer.Serialize(result.Data.DoNotSayConstraints),
                PossibleAnglesJson = JsonSerializer.Serialize(result.Data.PossibleAngles),
                LocalizationNotes = result.Data.LocalizationNotes,
                CreatedByEmail = actorEmail,
                UpdatedByEmail = actorEmail
            };
            dbContext.TruthSources.Add(truthSource);
        }
        item.UpdatedAtUtc = DateTime.UtcNow;
        item.UpdatedByEmail = actorEmail;
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.TruthSourceVersions.Add(new TruthSourceVersion
        {
            TruthSourceId = truthSource.Id,
            ContentItemId = item.Id,
            VersionNumber = truthSource.Version,
            SnapshotJson = JsonSerializer.Serialize(truthSource),
            SupportingEvidenceIdsJson = JsonSerializer.Serialize(new List<Guid> { evidence.Id }),
            ChangeSummary = "Borrador generado por análisis editorial de URL.",
            CreatedByEmail = actorEmail
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return await MapAsync(item, evidence, request.Angle, cancellationToken,
            EditorialUrlAnalysisStatus.Completed, null, null, result.Provider, result.IsSimulated);
    }

    public async Task<EditorialUrlAnalysisDto?> GetAsync(Guid analysisId, CancellationToken cancellationToken = default)
    {
        var evidence = await dbContext.ContentItemEvidences.AsNoTracking().FirstOrDefaultAsync(e => e.ContentItemId == analysisId, cancellationToken);
        var item = evidence == null ? null : await dbContext.ContentItems.AsNoTracking().FirstOrDefaultAsync(c => c.Id == analysisId, cancellationToken);
        return item == null || evidence == null ? null : await MapAsync(item, evidence, null, cancellationToken);
    }

    private async Task<EditorialUrlAnalysisDto> MapAsync(ContentItem item, ContentItemEvidence evidence, string? angle, CancellationToken cancellationToken,
        string? forcedStatus = null, string? errorCode = null, string? errorMessage = null, string? provider = null, bool simulated = false)
    {
        var notes = ParseNotes(evidence.Notes);
        var recommendation = await dbContext.AiRecommendations.AsNoTracking()
            .Where(r => r.ContentItemId == item.Id && r.Capability == AiCapabilities.BuildTruthSource)
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var truthSource = await dbContext.TruthSources.AsNoTracking().FirstOrDefaultAsync(t => t.ContentItemId == item.Id, cancellationToken);
        var draft = truthSource == null ? null : MapTruthSource(truthSource);
        var claims = truthSource == null ? [] : JsonSerializer.Deserialize<List<VerifiableClaimDto>>(truthSource.VerifiableClaimsJson) ?? [];
        var findings = claims.Select(c => new EditorialFindingDto(StableFindingId(c, c.EvidenceId ?? evidence.Id), c.Claim, c.Claim, c.EvidenceId ?? evidence.Id, c.Locator,
            c.ContentType, c.OriginalText, c.SpanishTranslation, c.Speaker, c.Timestamp, c.ReviewStatus)).ToList();
        var status = forcedStatus ?? (evidence.Status == EvidenceStatus.Captured && draft != null ? EditorialUrlAnalysisStatus.Completed :
            evidence.Status == EvidenceStatus.CaptureFailed ? EditorialUrlAnalysisStatus.Failed : EditorialUrlAnalysisStatus.NotAnalyzable);
        return new EditorialUrlAnalysisDto(item.Id, item.Id, item.ChannelId, status, errorCode ?? notes.ErrorCode, errorMessage ?? evidence.ErrorMessage,
            evidence.OriginUrl ?? string.Empty, angle ?? notes.Angle, evidence.Id, evidence.Title, notes.Language, evidence.Author,
            notes.TextOrigin, notes.Locator, draft, findings, provider ?? recommendation?.Provider ?? notes.Provider,
            simulated || recommendation?.IsSimulated == true || notes.Simulated, evidence.CapturedAtUtc);
    }

    private static VerifiableClaimDto VerifyClaim(VerifiableClaimDto claim, string sourceText)
    {
        if (claim.ContentType.Equals("Verbatim", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(claim.OriginalText) && sourceText.Contains(claim.OriginalText, StringComparison.Ordinal))
            return claim with { ReviewStatus = "Verified" };
        if (claim.ContentType.Equals("Verbatim", StringComparison.OrdinalIgnoreCase))
            return claim with { ContentType = "Paraphrase", OriginalText = null, Speaker = null, Timestamp = null, ReviewStatus = "NeedsReview" };
        return claim;
    }

    private static Guid StableFindingId(VerifiableClaimDto claim, Guid evidenceId)
    {
        var identity = $"{evidenceId:N}|{claim.Claim}|{claim.Locator}|{claim.ContentType}|{claim.OriginalText}";
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(identity));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static TruthSourceDto MapTruthSource(TruthSource ts) => new(
        ts.Id, ts.ContentItemId, ts.Status, ts.Summary,
        JsonSerializer.Deserialize<List<string>>(ts.KeyIdeasJson) ?? [],
        JsonSerializer.Deserialize<List<VerifiableClaimDto>>(ts.VerifiableClaimsJson) ?? [],
        JsonSerializer.Deserialize<List<Guid>>(ts.EvidenceReferencesJson) ?? [], ts.RiskNotes,
        JsonSerializer.Deserialize<List<string>>(ts.DoNotSayConstraintsJson) ?? [], JsonSerializer.Deserialize<List<string>>(ts.PossibleAnglesJson) ?? [],
        ts.LocalizationNotes, ts.RejectionReason, ts.RejectedAtUtc, ts.RejectedByEmail, ts.ApprovedAtUtc, ts.ApprovedByEmail,
        ts.Version, ts.CreatedAtUtc, ts.CreatedByEmail, ts.UpdatedAtUtc, ts.UpdatedByEmail);

    private static string NormalizeUrl(string value)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || string.IsNullOrWhiteSpace(uri.Host))
            throw new ArgumentException("Only absolute HTTP(S) URLs are supported.");
        if (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            (System.Net.IPAddress.TryParse(uri.Host, out var ip) && !IsPublic(ip)))
            throw new ArgumentException("The URL destination is not an allowed public address.");
        return new UriBuilder(uri) { Fragment = string.Empty }.Uri.ToString().TrimEnd('/');
    }

    private static bool IsPublic(System.Net.IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
            return IsPublic(ip.MapToIPv4());
        if (System.Net.IPAddress.IsLoopback(ip)) return false;
        var bytes = ip.GetAddressBytes();
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            return !(bytes[0] == 10 || bytes[0] == 127 || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
                (bytes[0] == 192 && bytes[1] == 168) || (bytes[0] == 169 && bytes[1] == 254));
        var isUniqueLocal = (bytes[0] & 0xFE) == 0xFC; // fc00::/7
        var isDocumentation = bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0D && bytes[3] == 0xB8; // 2001:db8::/32
        return !ip.Equals(System.Net.IPAddress.IPv6Any) && !ip.IsIPv6LinkLocal && !ip.IsIPv6SiteLocal &&
            !ip.IsIPv6Multicast && !isUniqueLocal && !isDocumentation;
    }

    private static string BuildSlug(string title) => new string(title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
    private static string BuildNotes(string? angle, string origin) => JsonSerializer.Serialize(new { angle, textOrigin = origin });
    private static string SanitizeError(string? error) => string.IsNullOrWhiteSpace(error) ? "Capture failed." : error.Length > 256 ? error[..256] : error;
    private static AnalysisNotes ParseNotes(string? notes)
    {
        try
        {
            return JsonSerializer.Deserialize<AnalysisNotes>(notes ?? "{}", new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new();
        }
        catch { return new(); }
    }
    private sealed record AnalysisNotes(string? Angle = null, string? TextOrigin = null, string? Locator = null, string? Language = null, string? Provider = null, bool Simulated = false, string? ErrorCode = null);
}
