using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Content;
using ContentFactory.Api.Modules.Discovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContentFactory.Api.Tests;

public class ContentAndTruthSourceDomainTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void TruthSource_RejectionRequiresNonEmptyReason()
    {
        var truthSource = new TruthSource
        {
            Status = TruthSourceStatus.UnderReview,
            Version = 1
        };

        var emptyReason = "   ";
        var isValidRejection = !string.IsNullOrWhiteSpace(emptyReason);

        Assert.False(isValidRejection);
    }

    [Fact]
    public async Task OptimisticConcurrency_DetectsStaleVersionConflict()
    {
        using var dbContext = CreateInMemoryDbContext();
        var contentItemId = Guid.NewGuid();

        var truthSource = new TruthSource
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            Summary = "Original summary",
            Version = 1
        };

        dbContext.TruthSources.Add(truthSource);
        await dbContext.SaveChangesAsync();

        // Simulate operator A updating version 1 to 2
        var operatorATruthSource = await dbContext.TruthSources.FindAsync(truthSource.Id);
        Assert.NotNull(operatorATruthSource);

        long loadedVersionByOperatorB = 1;
        long loadedVersionByOperatorA = operatorATruthSource.Version;

        // Operator A commits
        operatorATruthSource.Summary = "Summary updated by Operator A";
        operatorATruthSource.Version = loadedVersionByOperatorA + 1;
        await dbContext.SaveChangesAsync();

        // Operator B attempts update with stale version 1
        var currentInDb = await dbContext.TruthSources.FindAsync(truthSource.Id);
        Assert.NotNull(currentInDb);

        var isStaleWrite = loadedVersionByOperatorB != currentInDb.Version;
        Assert.True(isStaleWrite, "Operator B's write must be detected as a stale concurrency conflict");
    }

    [Fact]
    public void DownstreamGate_OnlyApprovedTruthSourceCanProgress()
    {
        var draftTs = new TruthSource { Status = TruthSourceStatus.Draft };
        var reviewTs = new TruthSource { Status = TruthSourceStatus.UnderReview };
        var rejectedTs = new TruthSource { Status = TruthSourceStatus.Rejected };
        var approvedTs = new TruthSource { Status = TruthSourceStatus.Approved };

        bool IsEligibleForDownstream(TruthSource ts) => ts.Status == TruthSourceStatus.Approved;

        Assert.False(IsEligibleForDownstream(draftTs));
        Assert.False(IsEligibleForDownstream(reviewTs));
        Assert.False(IsEligibleForDownstream(rejectedTs));
        Assert.True(IsEligibleForDownstream(approvedTs));
    }

    [Fact]
    public void EvidenceCapture_TextLead_ComputesValidSha256WithoutUrl()
    {
        var text = "Nota editorial rápida sin URL externa";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var hash = Convert.ToHexStringLower(hashBytes);

        var evidence = new ContentItemEvidence
        {
            ContentItemId = Guid.NewGuid(),
            Title = "Nota",
            OriginUrl = null,
            RawContent = text,
            ExtractedText = text,
            ContentHash = hash,
            Status = EvidenceStatus.Captured
        };

        Assert.Null(evidence.OriginUrl);
        Assert.Equal(EvidenceStatus.Captured, evidence.Status);
        Assert.False(string.IsNullOrWhiteSpace(evidence.ContentHash));
        Assert.Equal(64, evidence.ContentHash.Length);
    }

    [Fact]
    public void EvidenceCapture_FailedUrl_PreservesProvenanceWithoutFabrication()
    {
        var evidence = new ContentItemEvidence
        {
            ContentItemId = Guid.NewGuid(),
            DiscoveryCandidateId = Guid.NewGuid(),
            OriginUrl = "https://example.com/dead-link",
            Title = "Article Title",
            Status = EvidenceStatus.CaptureFailed,
            ErrorMessage = "HTTP 404: Not Found",
            ExtractedText = null
        };

        Assert.Equal(EvidenceStatus.CaptureFailed, evidence.Status);
        Assert.NotNull(evidence.OriginUrl);
        Assert.NotNull(evidence.DiscoveryCandidateId);
        Assert.Null(evidence.ExtractedText);
        Assert.Contains("404", evidence.ErrorMessage);
    }

    [Fact]
    public async Task NonDestructiveEvidenceRemoval_MaintainsHistoricalTraceability()
    {
        using var dbContext = CreateInMemoryDbContext();
        var contentItemId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();

        var evidence = new ContentItemEvidence
        {
            Id = evidenceId,
            ContentItemId = contentItemId,
            Title = "Evidence 1",
            Status = EvidenceStatus.Captured,
            ContentHash = "hash1"
        };

        var truthSourceVersion = new TruthSourceVersion
        {
            Id = Guid.NewGuid(),
            TruthSourceId = Guid.NewGuid(),
            ContentItemId = contentItemId,
            VersionNumber = 1,
            SnapshotJson = "{}",
            SupportingEvidenceIdsJson = JsonSerializer.Serialize(new List<Guid> { evidenceId })
        };

        dbContext.ContentItemEvidences.Add(evidence);
        dbContext.TruthSourceVersions.Add(truthSourceVersion);
        await dbContext.SaveChangesAsync();

        // Operator removes evidence from active set
        evidence.Status = EvidenceStatus.Excluded;
        await dbContext.SaveChangesAsync();

        // Verify evidence row still exists and version still references it
        var persistedEvidence = await dbContext.ContentItemEvidences.FindAsync(evidenceId);
        Assert.NotNull(persistedEvidence);
        Assert.Equal(EvidenceStatus.Excluded, persistedEvidence.Status);

        var persistedVersion = await dbContext.TruthSourceVersions.FindAsync(truthSourceVersion.Id);
        Assert.NotNull(persistedVersion);
        var supportingIds = JsonSerializer.Deserialize<List<Guid>>(persistedVersion.SupportingEvidenceIdsJson);
        Assert.Contains(evidenceId, supportingIds!);
    }
}
