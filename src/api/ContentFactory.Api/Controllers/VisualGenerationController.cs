using System.Security.Claims;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Infrastructure.Storage;
using ContentFactory.Api.Modules.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Controllers;

[ApiController]
[Route("api/v1/content-items/{contentItemId:guid}/storyboards/{storyboardId:guid}/visual-generation")]
[Route("api/content-items/{contentItemId:guid}/storyboards/{storyboardId:guid}/visual-generation")]
[Authorize]
public class VisualGenerationDispatchController(IVisualGenerationService visualService) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<List<JobDto>>> DispatchVisualGeneration(
        Guid contentItemId,
        Guid storyboardId,
        [FromBody] DispatchVisualGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        var result = await visualService.DispatchGenerationAsync(contentItemId, storyboardId, request, email, cancellationToken);

        if (!result.Success)
        {
            if (result.BlockerReason?.Contains("stale", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Conflict(new { error = result.BlockerReason });
            }

            return BadRequest(new { error = result.BlockerReason });
        }

        return Accepted(result.Jobs);
    }

    private string GetCurrentUserEmail() => User.FindFirstValue(ClaimTypes.Email) ?? "anonymous";
}

[ApiController]
[Route("api/v1/content-items/{contentItemId:guid}/storyboards/{storyboardId:guid}/visual-assets")]
[Route("api/content-items/{contentItemId:guid}/storyboards/{storyboardId:guid}/visual-assets")]
[Authorize]
public class VisualAssetsOverviewController(IVisualGenerationService visualService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<VisualProductionOverviewDto>> GetVisualAssetsOverview(
        Guid contentItemId,
        Guid storyboardId,
        CancellationToken cancellationToken)
    {
        var overview = await visualService.GetProductionOverviewAsync(contentItemId, storyboardId, cancellationToken);
        if (overview == null) return NotFound(new { error = "Storyboard not found." });
        return Ok(overview);
    }
}

[ApiController]
[Route("api/v1/jobs")]
[Route("api/jobs")]
[Authorize]
public class JobsController(IVisualGenerationService visualService) : ControllerBase
{
    [HttpGet("{jobId:guid}")]
    public async Task<ActionResult<JobDto>> GetJob(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await visualService.GetJobAsync(jobId, cancellationToken);
        if (job == null) return NotFound(new { error = "Job not found." });
        return Ok(job);
    }

    [HttpPost("{jobId:guid}/retry")]
    [Authorize(Policy = "RequireTechnical")]
    public async Task<ActionResult<JobDto>> RetryJob(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        var job = await visualService.RetryJobAsync(jobId, email, cancellationToken);
        if (job == null) return NotFound(new { error = "Job not found." });
        return Ok(job);
    }

    private string GetCurrentUserEmail() => User.FindFirstValue(ClaimTypes.Email) ?? "anonymous";
}

[ApiController]
[Route("api/v1/generated-assets")]
[Route("api/generated-assets")]
[Authorize]
public class GeneratedAssetsController(
    IVisualGenerationService visualService,
    IStorageService storageService,
    AppDbContext dbContext) : ControllerBase
{
    [HttpPost("{generatedAssetId:guid}/review")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<GeneratedAssetDto>> ReviewCandidate(
        Guid generatedAssetId,
        [FromBody] ReviewGeneratedAssetRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var asset = await visualService.ReviewCandidateAsync(generatedAssetId, request, email, cancellationToken);
            if (asset == null) return NotFound(new { error = "Generated asset not found." });
            return Ok(asset);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{generatedAssetId:guid}/select")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<GeneratedAssetDto>> SelectCandidate(
        Guid generatedAssetId,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        var asset = await visualService.SelectCandidateForAssemblyAsync(generatedAssetId, email, cancellationToken);
        if (asset == null) return NotFound(new { error = "Generated asset not found." });
        return Ok(asset);
    }

    [HttpGet("{generatedAssetId:guid}/stream")]
    public async Task<IActionResult> StreamGeneratedAsset(
        Guid generatedAssetId,
        CancellationToken cancellationToken)
    {
        var asset = await dbContext.GeneratedAssets.FirstOrDefaultAsync(ga => ga.Id == generatedAssetId, cancellationToken);
        if (asset == null) return NotFound(new { error = "Generated asset not found." });

        var download = await storageService.DownloadAsync(asset.StorageKey, cancellationToken);
        if (!download.Success || download.Stream == null)
        {
            return NotFound(new { error = "Media binary not found in storage." });
        }

        return File(download.Stream, download.ContentType, enableRangeProcessing: true);
    }

    [HttpGet("{generatedAssetId:guid}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(
        Guid generatedAssetId,
        CancellationToken cancellationToken)
    {
        return await StreamGeneratedAsset(generatedAssetId, cancellationToken);
    }

    private string GetCurrentUserEmail() => User.FindFirstValue(ClaimTypes.Email) ?? "anonymous";
}
