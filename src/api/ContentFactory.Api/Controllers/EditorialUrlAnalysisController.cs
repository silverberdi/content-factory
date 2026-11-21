using System.Security.Claims;
using ContentFactory.Api.Modules.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentFactory.Api.Controllers;

[ApiController]
[Route("api/editorial-url-analyses")]
[Authorize]
public sealed class EditorialUrlAnalysisController(IEditorialUrlAnalysisService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<EditorialUrlAnalysisDto>> Analyze(
        [FromBody] EditorialUrlAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.AnalyzeAsync(request, CurrentEmail(), cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { code = "INVALID_ANALYSIS_REQUEST", message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EditorialUrlAnalysisDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    private string CurrentEmail() => User.FindFirstValue(ClaimTypes.Email) ?? "anonymous";
}
