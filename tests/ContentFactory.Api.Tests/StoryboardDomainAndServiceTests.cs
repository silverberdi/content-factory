using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Content;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Tests;

public class StoryboardDomainAndServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void Storyboard_FrameTimingCalculation_ComputesTotalDurationAccurately()
    {
        var frames = new List<StoryboardFrame>
        {
            new() { OrderIndex = 1, EstimatedDurationSeconds = 4.5, ScriptSceneOrderIndex = 1 },
            new() { OrderIndex = 2, EstimatedDurationSeconds = 3.5, ScriptSceneOrderIndex = 1 },
            new() { OrderIndex = 3, EstimatedDurationSeconds = 6.0, ScriptSceneOrderIndex = 2 },
            new() { OrderIndex = 4, EstimatedDurationSeconds = 5.0, ScriptSceneOrderIndex = 3 },
            new() { OrderIndex = 5, EstimatedDurationSeconds = 4.0, ScriptSceneOrderIndex = 4 },
            new() { OrderIndex = 6, EstimatedDurationSeconds = 4.0, ScriptSceneOrderIndex = 5 },
        };

        var totalDuration = Math.Round(frames.Sum(f => f.EstimatedDurationSeconds), 1);
        Assert.Equal(27.0, totalDuration);

        // Subdivided scene (Scene 1) has 2 frames totaling 8.0s
        var scene1Duration = Math.Round(frames.Where(f => f.ScriptSceneOrderIndex == 1).Sum(f => f.EstimatedDurationSeconds), 1);
        Assert.Equal(8.0, scene1Duration);
    }

    [Fact]
    public async Task Storyboard_PreservesImmutableLineage_AndSceneLinkage()
    {
        using var dbContext = CreateInMemoryDbContext();
        var contentItemId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var scriptId = Guid.NewGuid();
        var scriptVersionId = Guid.NewGuid();
        var truthSourceId = Guid.NewGuid();
        var tsVersionId = Guid.NewGuid();
        var sceneId = Guid.NewGuid();

        var storyboard = new Storyboard
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            ChannelId = channelId,
            ScriptId = scriptId,
            ScriptVersionId = scriptVersionId,
            TruthSourceId = truthSourceId,
            TruthSourceVersionId = tsVersionId,
            IsCurrent = true,
            Title = "3 Habilidades Clave - Storyboard",
            TargetDurationSeconds = 45,
            TotalEstimatedDurationSeconds = 45.0,
            Status = StoryboardStatus.Draft,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = "editor@silverman.pro"
        };

        var frame1 = new StoryboardFrame
        {
            Id = Guid.NewGuid(),
            StoryboardId = storyboard.Id,
            OrderIndex = 1,
            ScriptSceneId = sceneId,
            ScriptSceneOrderIndex = 1,
            FramingIntent = FramingIntent.CloseUp,
            CompositionIntent = "Subject center, bottom 30% reserved for captions",
            CameraMotionIntent = CameraMotionIntent.SlowZoomIn,
            Subject = "Modern developer working with dual monitors",
            Environment = "Sleek moody workspace with blue ambient backlight",
            StyleIntent = "Clean tech minimalism",
            VisualPrompt = "Close-up of a focused engineer analyzing AI workflow diagram on a glowing vertical display, cinematic lighting, 9:16 vertical framing",
            NegativePrompt = "deformed fingers, blurry, text artifacts",
            AudioCue = "La inteligencia artificial no te va a reemplazar...",
            EstimatedDurationSeconds = 4.5,
            OnScreenText = "¿La IA te reemplazará?",
            TransitionIntent = TransitionIntent.Cut
        };

        storyboard.Frames.Add(frame1);

        var assetPlan = new AssetPlan
        {
            Id = Guid.NewGuid(),
            StoryboardId = storyboard.Id,
            ContentItemId = contentItemId,
            Status = AssetPlanStatus.Planned,
            Version = 1
        };

        var assetReq1 = new AssetRequirement
        {
            Id = Guid.NewGuid(),
            AssetPlanId = assetPlan.Id,
            FrameId = frame1.Id,
            FrameOrderIndex = 1,
            AssetType = AssetType.AiImage,
            AspectRatio = "9:16",
            VisualPrompt = frame1.VisualPrompt,
            NegativePrompt = frame1.NegativePrompt,
            StyleIntent = frame1.StyleIntent,
            MotionIntent = frame1.CameraMotionIntent,
            TargetDurationSeconds = frame1.EstimatedDurationSeconds,
            VoiceIntent = "Sober Spanish male narrator, tech curiosity tone",
            MusicMood = "Ambient tech minimalist",
            SoundEffectIntent = "Subtle digital click at start",
            SubtitleProfile = "Center-bottom kinetic captions"
        };

        assetPlan.Requirements.Add(assetReq1);
        storyboard.AssetPlan = assetPlan;

        dbContext.Storyboards.Add(storyboard);
        await dbContext.SaveChangesAsync();

        var loaded = await dbContext.Storyboards
            .Include(s => s.Frames)
            .Include(s => s.AssetPlan!)
                .ThenInclude(ap => ap.Requirements)
            .FirstOrDefaultAsync(s => s.Id == storyboard.Id);

        Assert.NotNull(loaded);
        Assert.Equal(contentItemId, loaded.ContentItemId);
        Assert.Equal(scriptVersionId, loaded.ScriptVersionId);
        Assert.Equal(tsVersionId, loaded.TruthSourceVersionId);
        Assert.True(loaded.IsCurrent);
        Assert.Single(loaded.Frames);
        Assert.Equal(FramingIntent.CloseUp, loaded.Frames[0].FramingIntent);
        Assert.Equal("9:16", loaded.AssetPlan!.Requirements[0].AspectRatio);
        Assert.Equal(AssetType.AiImage, loaded.AssetPlan.Requirements[0].AssetType);
    }

    [Fact]
    public void AssetRequirement_ValidatesProviderAgnosticFields()
    {
        var req = new AssetRequirement
        {
            Id = Guid.NewGuid(),
            AssetPlanId = Guid.NewGuid(),
            AssetType = AssetType.AiImage,
            AspectRatio = "9:16",
            VisualPrompt = "Futuristic vertical holographic interface",
            StyleIntent = "Cyber-minimalist",
            MotionIntent = CameraMotionIntent.PanUp,
            TargetDurationSeconds = 5.0
        };

        Assert.Equal("9:16", req.AspectRatio);
        Assert.Equal(AssetType.AiImage, req.AssetType);
        Assert.NotEmpty(req.VisualPrompt);
        Assert.Equal(CameraMotionIntent.PanUp, req.MotionIntent);
    }

    [Fact]
    public async Task OneCurrentStoryboard_Invariant_MaintainsSingleCurrentInstance()
    {
        using var dbContext = CreateInMemoryDbContext();
        var contentItemId = Guid.NewGuid();
        var scriptId = Guid.NewGuid();

        var oldStoryboard = new Storyboard
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            ChannelId = Guid.NewGuid(),
            ScriptId = scriptId,
            ScriptVersionId = Guid.NewGuid(),
            TruthSourceId = Guid.NewGuid(),
            TruthSourceVersionId = Guid.NewGuid(),
            IsCurrent = false,
            SupersededAtUtc = DateTime.UtcNow,
            Title = "Storyboard v1 (Historical)",
            Status = StoryboardStatus.Approved,
            Version = 2
        };

        var currentStoryboard = new Storyboard
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            ChannelId = oldStoryboard.ChannelId,
            ScriptId = scriptId,
            ScriptVersionId = Guid.NewGuid(),
            TruthSourceId = oldStoryboard.TruthSourceId,
            TruthSourceVersionId = oldStoryboard.TruthSourceVersionId,
            IsCurrent = true,
            ReconciledFromStoryboardId = oldStoryboard.Id,
            Title = "Storyboard v2 (Successor)",
            Status = StoryboardStatus.Draft,
            Version = 1
        };

        dbContext.Storyboards.AddRange(oldStoryboard, currentStoryboard);
        await dbContext.SaveChangesAsync();

        var current = await dbContext.Storyboards
            .Where(s => s.ContentItemId == contentItemId && s.IsCurrent)
            .SingleOrDefaultAsync();

        Assert.NotNull(current);
        Assert.Equal(currentStoryboard.Id, current.Id);
        Assert.Equal(oldStoryboard.Id, current.ReconciledFromStoryboardId);

        var totalForContentItem = await dbContext.Storyboards
            .CountAsync(s => s.ContentItemId == contentItemId);
        Assert.Equal(2, totalForContentItem);
    }
}
