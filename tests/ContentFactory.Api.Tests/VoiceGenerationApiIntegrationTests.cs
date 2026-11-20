using System.Security.Claims;
using ContentFactory.Api.Controllers;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Infrastructure.Storage;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContentFactory.Api.Tests;

public class VoiceGenerationApiIntegrationTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static (VoiceGenerationService voiceService, VisualGenerationService visualService, MinioStorageService storage, AppDbContext dbContext) CreateTestSetup()
    {
        var dbContext = CreateInMemoryDbContext();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["VOICE_PROVIDER"] = "Mock" })
            .Build();
        var storage = new MinioStorageService(config, new TestHttpClientFactory(), NullLogger<MinioStorageService>.Instance);
        var mockVoiceProvider = new MockVoiceGenerationProvider(NullLogger<MockVoiceGenerationProvider>.Instance);
        var mockVisualProvider = new MockVisualGenerationProvider(NullLogger<MockVisualGenerationProvider>.Instance);

        var voiceService = new VoiceGenerationService(
            dbContext,
            storage,
            [mockVoiceProvider],
            config,
            NullLogger<VoiceGenerationService>.Instance
        );

        var visualService = new VisualGenerationService(
            dbContext,
            storage,
            [mockVisualProvider],
            config,
            NullLogger<VisualGenerationService>.Instance
        );

        return (voiceService, visualService, storage, dbContext);
    }

    private static void SetUserContext(ControllerBase controller, string email = "editor@silverman.pro", string role = "EDITORIAL")
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        ], "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    private static async Task<(ContentItem item, Storyboard storyboard, StoryboardVersion sbVer, AssetRequirement req)> SeedStoryboardPrerequisitesAsync(AppDbContext dbContext, bool isApproved = true, bool isStale = false)
    {
        var channel = new Channel { Id = Guid.NewGuid(), Name = "IA Simple ES", Slug = "ia-simple-es", Language = "es", Niche = "AI" };
        var contentItem = new ContentItem { Id = Guid.NewGuid(), ChannelId = channel.Id, Title = "Voice Piece 1", Slug = "voice-piece-1", Stage = ContentItemStage.StoryboardApproved };

        var scriptId = Guid.NewGuid();
        var scriptVerId = Guid.NewGuid();

        var script = new Script
        {
            Id = scriptId,
            ContentItemId = contentItem.Id,
            ChannelId = channel.Id,
            Status = ScriptStatus.Approved,
            EstimatedDurationSeconds = 45.0
        };

        var scene = new ScriptScene
        {
            Id = Guid.NewGuid(),
            ScriptId = scriptId,
            OrderIndex = 1,
            SceneType = SceneType.Hook,
            NarrationText = "Bienvenidos a este nuevo episodio de inteligencia artificial.",
            WordCount = 8,
            EstimatedDurationSeconds = 3.5
        };
        script.Scenes.Add(scene);

        var scriptVersion = new ScriptVersion
        {
            Id = isStale ? Guid.NewGuid() : scriptVerId,
            ScriptId = scriptId,
            VersionNumber = 1,
            Status = ScriptStatus.Approved
        };

        var storyboardId = Guid.NewGuid();
        var storyboard = new Storyboard
        {
            Id = storyboardId,
            ContentItemId = contentItem.Id,
            ChannelId = channel.Id,
            ScriptId = scriptId,
            ScriptVersionId = scriptVerId,
            IsCurrent = true,
            Status = isApproved ? StoryboardStatus.Approved : StoryboardStatus.Draft,
            Title = "Test Storyboard",
            Version = 1
        };

        var assetPlan = new AssetPlan
        {
            Id = Guid.NewGuid(),
            StoryboardId = storyboardId,
            ContentItemId = contentItem.Id,
            Status = isApproved ? AssetPlanStatus.ReadyForGeneration : AssetPlanStatus.Planned
        };

        var voiceReq = new AssetRequirement
        {
            Id = Guid.NewGuid(),
            AssetPlanId = assetPlan.Id,
            AssetType = AssetType.TtsVoiceover,
            VoiceIntent = "Clear Spanish voiceover"
        };
        assetPlan.Requirements.Add(voiceReq);
        storyboard.AssetPlan = assetPlan;

        var sbVer = new StoryboardVersion
        {
            Id = Guid.NewGuid(),
            StoryboardId = storyboardId,
            ContentItemId = contentItem.Id,
            ScriptId = scriptId,
            ScriptVersionId = scriptVerId,
            VersionNumber = 1,
            Status = isApproved ? StoryboardStatus.Approved : StoryboardStatus.Draft
        };

        dbContext.Channels.Add(channel);
        dbContext.ContentItems.Add(contentItem);
        dbContext.Scripts.Add(script);
        dbContext.ScriptVersions.Add(scriptVersion);
        dbContext.Storyboards.Add(storyboard);
        dbContext.StoryboardVersions.Add(sbVer);
        await dbContext.SaveChangesAsync();

        return (contentItem, storyboard, sbVer, voiceReq);
    }

    [Fact]
    public async Task DispatchVoiceGeneration_ReturnsAccepted()
    {
        var (voiceService, _, _, dbContext) = CreateTestSetup();
        var (item, sb, _, req) = await SeedStoryboardPrerequisitesAsync(dbContext);

        var controller = new VoiceGenerationDispatchController(voiceService);
        SetUserContext(controller, "editor@silverman.pro", "EDITORIAL");

        var actionResult = await controller.DispatchVoiceGeneration(
            item.Id,
            sb.Id,
            new DispatchVoiceGenerationRequest(req.Id, CandidateCount: 1),
            CancellationToken.None
        );

        var accepted = Assert.IsType<AcceptedResult>(actionResult.Result);
        var jobs = Assert.IsType<List<JobDto>>(accepted.Value);
        Assert.Single(jobs);
        Assert.Equal(JobStatus.Queued, jobs[0].Status);
    }

    [Fact]
    public async Task GetVoiceAssetsOverview_ReturnsOverviewDto()
    {
        var (voiceService, _, _, dbContext) = CreateTestSetup();
        var (item, sb, _, _) = await SeedStoryboardPrerequisitesAsync(dbContext);

        var controller = new VoiceAssetsOverviewController(voiceService);
        SetUserContext(controller, "editor@silverman.pro", "EDITORIAL");

        var actionResult = await controller.GetVoiceAssetsOverview(item.Id, sb.Id, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var overview = Assert.IsType<VoiceProductionOverviewDto>(ok.Value);

        Assert.Equal(item.Id, overview.ContentItemId);
        Assert.Equal(sb.Id, overview.StoryboardId);
        Assert.True(overview.IsEligibleForGeneration);
        Assert.Single(overview.Requirements);
        Assert.Single(overview.ScriptScenes);
    }

    [Fact]
    public async Task ReviewCandidate_And_SelectCandidate_Flow()
    {
        var (voiceService, visualService, storage, dbContext) = CreateTestSetup();
        var (item, sb, _, req) = await SeedStoryboardPrerequisitesAsync(dbContext);

        // Dispatch and execute
        await voiceService.DispatchGenerationAsync(item.Id, sb.Id, new DispatchVoiceGenerationRequest(req.Id, CandidateCount: 2), "editor@silverman.pro");
        await voiceService.ProcessQueuedJobsAsync();

        var candidates = await dbContext.GeneratedAssets.Where(ga => ga.AssetRequirementId == req.Id).ToListAsync();
        Assert.Equal(2, candidates.Count);

        var genAssetsController = new GeneratedAssetsController(visualService, voiceService, storage, dbContext);
        SetUserContext(genAssetsController, "editor@silverman.pro", "EDITORIAL");

        // Approve Candidate 1
        var reviewRes = await genAssetsController.ReviewCandidate(
            candidates[0].Id,
            new ReviewGeneratedAssetRequest(GeneratedAssetStatus.Approved, null),
            CancellationToken.None
        );

        var okReview = Assert.IsType<OkObjectResult>(reviewRes.Result);
        var dto = Assert.IsType<GeneratedAssetDto>(okReview.Value);
        Assert.Equal(GeneratedAssetStatus.Approved, dto.Status);
        Assert.True(dto.IsSelectedForAssembly);

        // Select Candidate 2 (after approving it)
        await genAssetsController.ReviewCandidate(
            candidates[1].Id,
            new ReviewGeneratedAssetRequest(GeneratedAssetStatus.Approved, null),
            CancellationToken.None
        );

        var selectRes = await genAssetsController.SelectCandidate(candidates[1].Id, CancellationToken.None);
        var okSelect = Assert.IsType<OkObjectResult>(selectRes.Result);
        var dto2 = Assert.IsType<GeneratedAssetDto>(okSelect.Value);
        Assert.True(dto2.IsSelectedForAssembly);

        var cand1 = await dbContext.GeneratedAssets.FirstAsync(ga => ga.Id == candidates[0].Id);
        Assert.False(cand1.IsSelectedForAssembly);
    }

    [Fact]
    public async Task VisualService_CannotMutateVoiceCandidate_AndVisualReviewStillWorks()
    {
        var (voiceService, visualService, _, dbContext) = CreateTestSetup();
        var (item, sb, _, voiceReq) = await SeedStoryboardPrerequisitesAsync(dbContext);
        await voiceService.DispatchGenerationAsync(item.Id, sb.Id, new DispatchVoiceGenerationRequest(voiceReq.Id, CandidateCount: 1), "editor@silverman.pro");
        await voiceService.ProcessQueuedJobsAsync();
        var voiceCandidate = await dbContext.GeneratedAssets.SingleAsync(asset => asset.AssetRequirementId == voiceReq.Id);

        var visualAttempt = await visualService.ReviewCandidateAsync(voiceCandidate.Id, new ReviewGeneratedAssetRequest(GeneratedAssetStatus.Approved, null), "editor@silverman.pro");
        Assert.Null(visualAttempt);
        Assert.Equal(GeneratedAssetStatus.PendingReview, (await dbContext.GeneratedAssets.FindAsync(voiceCandidate.Id))!.Status);

        var visualCandidate = new GeneratedAsset
        {
            Id = Guid.NewGuid(), ContentItemId = item.Id, ChannelId = item.ChannelId, StoryboardId = sb.Id,
            AssetRequirementId = Guid.NewGuid(), JobId = Guid.NewGuid(), AssetType = AssetType.AiImage,
            Status = GeneratedAssetStatus.PendingReview, StorageKey = "visual/test.png"
        };
        dbContext.GeneratedAssets.Add(visualCandidate);
        await dbContext.SaveChangesAsync();

        var reviewedVisual = await visualService.ReviewCandidateAsync(visualCandidate.Id, new ReviewGeneratedAssetRequest(GeneratedAssetStatus.Approved, null), "editor@silverman.pro");
        Assert.Equal(GeneratedAssetStatus.Approved, reviewedVisual!.Status);
    }

    [Fact]
    public async Task StreamGeneratedAsset_ServesAudioFile()
    {
        var (voiceService, visualService, storage, dbContext) = CreateTestSetup();
        var (item, sb, _, req) = await SeedStoryboardPrerequisitesAsync(dbContext);

        await voiceService.DispatchGenerationAsync(item.Id, sb.Id, new DispatchVoiceGenerationRequest(req.Id, CandidateCount: 1), "editor@silverman.pro");
        await voiceService.ProcessQueuedJobsAsync();

        var candidate = await dbContext.GeneratedAssets.FirstAsync(ga => ga.AssetRequirementId == req.Id);

        var genAssetsController = new GeneratedAssetsController(visualService, voiceService, storage, dbContext);
        SetUserContext(genAssetsController, "editor@silverman.pro", "EDITORIAL");

        var streamResult = await genAssetsController.StreamGeneratedAsset(candidate.Id, CancellationToken.None);
        var fileResult = Assert.IsType<FileStreamResult>(streamResult);
        Assert.Equal("audio/wav", fileResult.ContentType);
        Assert.True(fileResult.EnableRangeProcessing);
    }

    [Fact]
    public async Task RetryJob_Succeeds_ForTechnicalRole()
    {
        var (voiceService, visualService, _, dbContext) = CreateTestSetup();
        var (item, sb, _, req) = await SeedStoryboardPrerequisitesAsync(dbContext);

        var job = new Job
        {
            Id = Guid.NewGuid(),
            ContentItemId = item.Id,
            ChannelId = item.ChannelId,
            StoryboardId = sb.Id,
            SourceAssetRequirementId = req.Id,
            JobType = JobTypes.GenerateVoiceAsset,
            Status = JobStatus.FailedActionRequired,
            ErrorCode = "INVALID_VOICE_CONFIGURATION",
            SanitizedErrorMessage = "Simulated error"
        };
        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync();

        var jobsController = new JobsController(visualService, voiceService);
        SetUserContext(jobsController, "admin@silverman.pro", "TECHNICAL");

        var result = await jobsController.RetryJob(job.Id, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var jobDto = Assert.IsType<JobDto>(ok.Value);

        Assert.Equal(JobStatus.Queued, jobDto.Status);
    }

    [Fact]
    public void EndpointAuthorizationAttributes_EnforceRolePolicies()
    {
        // 1. Voice dispatch requires Editorial
        var dispatchMethod = typeof(VoiceGenerationDispatchController).GetMethod(nameof(VoiceGenerationDispatchController.DispatchVoiceGeneration));
        var dispatchAuth = dispatchMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();
        Assert.NotNull(dispatchAuth);
        Assert.Equal("RequireEditorial", dispatchAuth.Policy);

        // 2. Review and Select candidate require Editorial
        var reviewMethod = typeof(GeneratedAssetsController).GetMethod(nameof(GeneratedAssetsController.ReviewCandidate));
        var reviewAuth = reviewMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();
        Assert.NotNull(reviewAuth);
        Assert.Equal("RequireEditorial", reviewAuth.Policy);

        var selectMethod = typeof(GeneratedAssetsController).GetMethod(nameof(GeneratedAssetsController.SelectCandidate));
        var selectAuth = selectMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();
        Assert.NotNull(selectAuth);
        Assert.Equal("RequireEditorial", selectAuth.Policy);

        // 3. Retry job requires Technical
        var retryMethod = typeof(JobsController).GetMethod(nameof(JobsController.RetryJob));
        var retryAuth = retryMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();
        Assert.NotNull(retryAuth);
        Assert.Equal("RequireTechnical", retryAuth.Policy);

        var getJobMethod = typeof(JobsController).GetMethod(nameof(JobsController.GetJob));
        var getJobAuth = getJobMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();
        Assert.NotNull(getJobAuth);
        Assert.Equal("RequireTechnical", getJobAuth.Policy);

        // 4. Stream and Voice overview require Editorial or Technical
        var streamMethod = typeof(GeneratedAssetsController).GetMethod(nameof(GeneratedAssetsController.StreamGeneratedAsset));
        var streamAuth = streamMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();
        Assert.NotNull(streamAuth);
        Assert.Equal("RequireEditorialOrTechnical", streamAuth.Policy);

        var overviewMethod = typeof(VoiceAssetsOverviewController).GetMethod(nameof(VoiceAssetsOverviewController.GetVoiceAssetsOverview));
        var overviewAuth = overviewMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();
        Assert.NotNull(overviewAuth);
        Assert.Equal("RequireEditorialOrTechnical", overviewAuth.Policy);
    }

    [Fact]
    public async Task LiveGeminiTts_FullPipeline_Dispatch_Worker_Storage_Playback()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_LIVE_GEMINI_TTS_TESTS"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Program.LoadEnvFile();
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "CHANGE_ME")
        {
            var rootEnvDev = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", ".env.development");
            if (File.Exists(rootEnvDev))
            {
                foreach (var line in File.ReadAllLines(rootEnvDev))
                {
                    if (line.StartsWith("GEMINI_API_KEY="))
                    {
                        apiKey = line.Split('=', 2)[1].Trim('"', '\'').Trim();
                        Environment.SetEnvironmentVariable("GEMINI_API_KEY", apiKey);
                        break;
                    }
                }
            }
        }

        Assert.False(string.IsNullOrWhiteSpace(apiKey) || apiKey == "CHANGE_ME", "GEMINI_API_KEY is required for live smoke test.");

        var dbContext = CreateInMemoryDbContext();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GEMINI_API_KEY"] = apiKey,
                ["GEMINI_TTS_MODEL"] = "gemini-3.8-flash-lite-tts",
                ["GEMINI_VOICE_NAME"] = "Kore",
                ["VOICE_PROVIDER"] = "Gemini",
                ["ENVIRONMENT"] = "development"
            })
            .Build();

        var storage = new MinioStorageService(config, new TestHttpClientFactory(), NullLogger<MinioStorageService>.Instance);
        var geminiProvider = new GeminiTtsProvider(new TestHttpClientFactory(), config, NullLogger<GeminiTtsProvider>.Instance);

        var voiceService = new VoiceGenerationService(
            dbContext,
            storage,
            [geminiProvider],
            config,
            NullLogger<VoiceGenerationService>.Instance
        );

        var (item, sb, _, req) = await SeedStoryboardPrerequisitesAsync(dbContext);

        // 1. Dispatch
        var dispatchResult = await voiceService.DispatchGenerationAsync(
            item.Id, sb.Id, new DispatchVoiceGenerationRequest(req.Id, CandidateCount: 1), "editor@silverman.pro");
        Assert.True(dispatchResult.Success, dispatchResult.BlockerReason);
        Assert.Single(dispatchResult.Jobs);

        var jobId = dispatchResult.Jobs[0].Id;
        var jobBefore = await dbContext.Jobs.FirstAsync(j => j.Id == jobId);
        Assert.Equal(JobStatus.Queued, jobBefore.Status);
        Assert.Equal(JobTypes.GenerateVoiceAsset, jobBefore.JobType);

        // 2. Worker Processing Loop
        await voiceService.ProcessQueuedJobsAsync(CancellationToken.None);

        var jobAfter = await dbContext.Jobs.Include(j => j.Attempts).FirstAsync(j => j.Id == jobId);
        Assert.Equal(JobStatus.Succeeded, jobAfter.Status);
        Assert.NotEmpty(jobAfter.Attempts);
        Assert.Equal(JobStatus.Succeeded, jobAfter.Attempts.Last().Status);

        // 3. Generated Asset Persistence
        var candidate = await dbContext.GeneratedAssets.FirstOrDefaultAsync(ga => ga.AssetRequirementId == req.Id);
        Assert.NotNull(candidate);

        Assert.Equal("Audio", candidate.MediaType);
        Assert.Equal("audio/wav", candidate.ContentType);
        Assert.True(candidate.DurationSeconds > 0);
        Assert.Equal(64, candidate.ChecksumSha256.Length);
        Assert.Equal("Gemini", candidate.Provider);
        Assert.Equal("gemini-3.8-flash-lite-tts:Kore", candidate.ProviderModelOrWorkflow);
        Assert.Contains("language", candidate.GenerationParametersSnapshot);

        // 4. Authenticated Storage & Playback Stream
        var downloadResult = await storage.DownloadAsync(candidate.StorageKey, CancellationToken.None);
        Assert.True(downloadResult.Success, downloadResult.ErrorMessage);
        Assert.NotNull(downloadResult.Stream);

        using var ms = new MemoryStream();
        await downloadResult.Stream!.CopyToAsync(ms);
        var bytes = ms.ToArray();

        Assert.True(bytes.Length > 44);
        Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.Equal("WAVE", System.Text.Encoding.ASCII.GetString(bytes, 8, 4));
        Assert.Equal("fmt ", System.Text.Encoding.ASCII.GetString(bytes, 12, 4));

        // 5. Absence of secrets
        Assert.DoesNotContain(apiKey, candidate.ProviderModelOrWorkflow);
        Assert.DoesNotContain(apiKey, candidate.GenerationParametersSnapshot);
        if (jobAfter.Attempts[0].ErrorMessage != null)
        {
            Assert.DoesNotContain(apiKey, jobAfter.Attempts[0].ErrorMessage);
        }
    }
}
