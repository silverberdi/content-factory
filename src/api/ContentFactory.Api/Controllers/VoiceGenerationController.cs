using System.Security.Claims;
using ContentFactory.Api.Modules.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentFactory.Api.Controllers;

[ApiController]
[Route("api/v1/content-items/{contentItemId:guid}/storyboards/{storyboardId:guid}/voice-generation")]
[Route("api/content-items/{contentItemId:guid}/storyboards/{storyboardId:guid}/voice-generation")]
[Authorize]
public class VoiceGenerationDispatchController(IVoiceGenerationService voiceService) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<List<JobDto>>> DispatchVoiceGeneration(
        Guid contentItemId,
        Guid storyboardId,
        [FromBody] DispatchVoiceGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        var result = await voiceService.DispatchGenerationAsync(contentItemId, storyboardId, request, email, cancellationToken);

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
[Route("api/v1/content-items/{contentItemId:guid}/storyboards/{storyboardId:guid}/voice-assets")]
[Route("api/content-items/{contentItemId:guid}/storyboards/{storyboardId:guid}/voice-assets")]
[Authorize]
public class VoiceAssetsOverviewController(IVoiceGenerationService voiceService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "RequireEditorialOrTechnical")]
    public async Task<ActionResult<VoiceProductionOverviewDto>> GetVoiceAssetsOverview(
        Guid contentItemId,
        Guid storyboardId,
        CancellationToken cancellationToken)
    {
        var overview = await voiceService.GetProductionOverviewAsync(contentItemId, storyboardId, cancellationToken);
        if (overview == null) return NotFound(new { error = "Storyboard not found." });
        return Ok(overview);
    }
}
