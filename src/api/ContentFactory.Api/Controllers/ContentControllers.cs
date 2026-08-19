using System.Security.Claims;
using ContentFactory.Api.Modules.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentFactory.Api.Controllers;

[ApiController]
[Route("api/content-items")]
[Authorize]
public class ContentItemsController(IContentService contentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ContentItemDto>>> GetItems(
        [FromQuery] Guid? channelId,
        [FromQuery] string? stage,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var items = await contentService.GetContentItemsAsync(channelId, stage, status, search, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContentItemDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await contentService.GetContentItemDetailAsync(id, cancellationToken);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ContentItemDto>> Create(
        [FromBody] CreateContentItemRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var item = await contentService.CreateContentItemAsync(request, email, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ContentItemDto>> Update(
        Guid id,
        [FromBody] UpdateContentItemRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var item = await contentService.UpdateContentItemAsync(id, request, email, cancellationToken);
            if (item == null) return NotFound();
            return Ok(item);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/evidence")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ContentItemEvidenceDto>> AttachEvidence(
        Guid id,
        [FromBody] AttachEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var evidence = await contentService.AttachEvidenceAsync(id, request, email, cancellationToken);
            return Ok(evidence);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/evidence/{evidenceId:guid}")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<IActionResult> DetachEvidence(
        Guid id,
        Guid evidenceId,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        var success = await contentService.DetachOrExcludeEvidenceAsync(id, evidenceId, email, cancellationToken);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPost("{id:guid}/evidence/{evidenceId:guid}/retry")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ContentItemEvidenceDto>> RetryEvidenceCapture(
        Guid id,
        Guid evidenceId,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        var evidence = await contentService.RetryEvidenceCaptureAsync(id, evidenceId, email, cancellationToken);
        if (evidence == null) return NotFound();
        return Ok(evidence);
    }

    private string GetCurrentUserEmail() => User.FindFirstValue(ClaimTypes.Email) ?? "anonymous";
}

[ApiController]
[Route("api/content-items/{contentItemId:guid}/truth-source")]
[Authorize]
public class TruthSourceController(ITruthSourceService truthSourceService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TruthSourceDto>> Get(Guid contentItemId, CancellationToken cancellationToken)
    {
        var ts = await truthSourceService.GetTruthSourceAsync(contentItemId, cancellationToken);
        if (ts == null) return NotFound();
        return Ok(ts);
    }

    [HttpPost("generate-ai-draft")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<TruthSourceDto>> GenerateAiDraft(Guid contentItemId, CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var ts = await truthSourceService.GenerateAiDraftAsync(contentItemId, email, cancellationToken);
            return Ok(ts);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<TruthSourceDto>> Save(
        Guid contentItemId,
        [FromBody] SaveTruthSourceRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var ts = await truthSourceService.SaveTruthSourceAsync(contentItemId, request, email, cancellationToken);
            return Ok(ts);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("submit-review")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<TruthSourceDto>> SubmitReview(Guid contentItemId, CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var ts = await truthSourceService.SubmitForReviewAsync(contentItemId, email, cancellationToken);
            return Ok(ts);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("approve")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<TruthSourceDto>> Approve(Guid contentItemId, CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var ts = await truthSourceService.ApproveTruthSourceAsync(contentItemId, email, cancellationToken);
            return Ok(ts);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("reject")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<TruthSourceDto>> Reject(
        Guid contentItemId,
        [FromBody] RejectTruthSourceRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var ts = await truthSourceService.RejectTruthSourceAsync(contentItemId, request, email, cancellationToken);
            return Ok(ts);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("versions")]
    public async Task<ActionResult<List<TruthSourceVersionDto>>> GetVersionHistory(Guid contentItemId, CancellationToken cancellationToken)
    {
        var versions = await truthSourceService.GetVersionHistoryAsync(contentItemId, cancellationToken);
        return Ok(versions);
    }

    private string GetCurrentUserEmail() => User.FindFirstValue(ClaimTypes.Email) ?? "anonymous";
}

[ApiController]
[Route("api/editorial-tasks")]
[Authorize]
public class EditorialTasksController(IEditorialTaskService editorialTaskService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<EditorialTaskDto>>> GetTasks(
        [FromQuery] Guid? channelId,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] string? assignedEmail,
        CancellationToken cancellationToken)
    {
        var tasks = await editorialTaskService.GetTasksAsync(channelId, status, priority, assignedEmail, cancellationToken);
        return Ok(tasks);
    }

    [HttpPut("{id:guid}/assign")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<EditorialTaskDto>> Assign(
        Guid id,
        [FromBody] AssignEditorialTaskRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        var task = await editorialTaskService.AssignTaskAsync(id, request, email, cancellationToken);
        if (task == null) return NotFound();
        return Ok(task);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<EditorialTaskDto>> UpdateStatus(
        Guid id,
        [FromBody] string status,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var task = await editorialTaskService.UpdateTaskStatusAsync(id, status, email, cancellationToken);
            if (task == null) return NotFound();
            return Ok(task);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private string GetCurrentUserEmail() => User.FindFirstValue(ClaimTypes.Email) ?? "anonymous";
}

[ApiController]
[Route("api/content-items/{contentItemId:guid}/ideas")]
[Authorize(Policy = "RequireEditorial")]
public class ContentIdeasController(IContentIdeaService ideaService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ContentIdeaDto>>> GetIdeas(
        Guid contentItemId,
        CancellationToken cancellationToken)
    {
        var ideas = await ideaService.GetIdeasByContentItemIdAsync(contentItemId, cancellationToken);
        return Ok(ideas);
    }

    [HttpGet("{ideaId:guid}")]
    public async Task<ActionResult<ContentIdeaDto>> GetIdeaById(
        Guid contentItemId,
        Guid ideaId,
        CancellationToken cancellationToken)
    {
        var idea = await ideaService.GetIdeaByIdAsync(contentItemId, ideaId, cancellationToken);
        if (idea == null) return NotFound();
        return Ok(idea);
    }

    [HttpGet("{ideaId:guid}/versions")]
    public async Task<ActionResult<List<ContentIdeaVersionDto>>> GetIdeaVersions(
        Guid contentItemId,
        Guid ideaId,
        CancellationToken cancellationToken)
    {
        var versions = await ideaService.GetIdeaVersionsAsync(contentItemId, ideaId, cancellationToken);
        return Ok(versions);
    }

    [HttpPost("generate")]
    [HttpPost("generate-ai-ideas")]
    public async Task<ActionResult<List<ContentIdeaDto>>> GenerateAiIdeas(
        Guid contentItemId,
        [FromBody] GenerateIdeasOptions? options,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var ideas = await ideaService.GenerateAiIdeasAsync(contentItemId, options, email, cancellationToken);
            return Ok(ideas);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ContentIdeaDto>> CreateManualIdea(
        Guid contentItemId,
        [FromBody] CreateIdeaRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var idea = await ideaService.CreateManualIdeaAsync(contentItemId, request, email, cancellationToken);
            return CreatedAtAction(nameof(GetIdeaById), new { contentItemId, ideaId = idea.Id }, idea);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{ideaId:guid}")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ContentIdeaDto>> UpdateIdea(
        Guid contentItemId,
        Guid ideaId,
        [FromBody] UpdateIdeaRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var idea = await ideaService.UpdateIdeaAsync(contentItemId, ideaId, request, email, cancellationToken);
            return Ok(idea);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{ideaId:guid}/select")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ContentIdeaDto>> SelectIdea(
        Guid contentItemId,
        Guid ideaId,
        [FromBody] SelectIdeaRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var idea = await ideaService.SelectIdeaAsync(contentItemId, ideaId, request, email, cancellationToken);
            return Ok(idea);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{ideaId:guid}/dismiss")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ContentIdeaDto>> DismissIdea(
        Guid contentItemId,
        Guid ideaId,
        [FromBody] DismissIdeaRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var idea = await ideaService.DismissIdeaAsync(contentItemId, ideaId, request, email, cancellationToken);
            return Ok(idea);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{ideaId:guid}/reopen")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ContentIdeaDto>> ReopenIdea(
        Guid contentItemId,
        Guid ideaId,
        [FromBody] ReopenIdeaRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var idea = await ideaService.ReopenIdeaAsync(contentItemId, ideaId, request, email, cancellationToken);
            return Ok(idea);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private string GetCurrentUserEmail() => User.FindFirstValue(ClaimTypes.Email) ?? "anonymous";
}

[ApiController]
[Route("api/v1/content-items/{contentItemId:guid}/script")]
[Route("api/content-items/{contentItemId:guid}/script")]
[Authorize]
public class ScriptsController(IScriptService scriptService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ScriptDto>> GetScript(
        Guid contentItemId,
        CancellationToken cancellationToken)
    {
        var script = await scriptService.GetScriptByContentItemIdAsync(contentItemId, cancellationToken);
        if (script == null) return NotFound(new { error = "No script found for this ContentItem." });
        return Ok(script);
    }

    [HttpGet("versions")]
    public async Task<ActionResult<List<ScriptVersionDto>>> GetScriptVersions(
        Guid contentItemId,
        [FromQuery] Guid? scriptId,
        CancellationToken cancellationToken)
    {
        Guid targetScriptId;
        if (scriptId.HasValue && scriptId.Value != Guid.Empty)
        {
            targetScriptId = scriptId.Value;
        }
        else
        {
            var script = await scriptService.GetScriptByContentItemIdAsync(contentItemId, cancellationToken);
            if (script == null) return Ok(new List<ScriptVersionDto>());
            targetScriptId = script.Id;
        }

        var versions = await scriptService.GetScriptVersionsAsync(contentItemId, targetScriptId, cancellationToken);
        return Ok(versions);
    }

    [HttpGet("versions/{versionId:guid}")]
    public async Task<ActionResult<ScriptVersionDto>> GetScriptVersion(
        Guid contentItemId,
        Guid versionId,
        [FromQuery] Guid? scriptId,
        CancellationToken cancellationToken)
    {
        Guid targetScriptId;
        if (scriptId.HasValue && scriptId.Value != Guid.Empty)
        {
            targetScriptId = scriptId.Value;
        }
        else
        {
            var script = await scriptService.GetScriptByContentItemIdAsync(contentItemId, cancellationToken);
            if (script == null) return NotFound(new { error = "Script not found." });
            targetScriptId = script.Id;
        }

        var version = await scriptService.GetScriptVersionAsync(contentItemId, targetScriptId, versionId, cancellationToken);
        if (version == null) return NotFound(new { error = "Script version not found." });
        return Ok(version);
    }

    [HttpPost]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ScriptDto>> CreateScript(
        Guid contentItemId,
        [FromBody] CreateScriptRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var script = await scriptService.CreateScriptAsync(contentItemId, request, email, cancellationToken);
            return CreatedAtAction(nameof(GetScript), new { contentItemId }, script);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{scriptId:guid}")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ScriptDto>> UpdateScript(
        Guid contentItemId,
        Guid scriptId,
        [FromBody] UpdateScriptRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var script = await scriptService.UpdateScriptAsync(contentItemId, scriptId, request, email, cancellationToken);
            return Ok(script);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("generate")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ScriptDto>> GenerateAiScript(
        Guid contentItemId,
        [FromBody] GenerateScriptOptions? options,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var script = await scriptService.GenerateAiScriptAsync(contentItemId, options, email, cancellationToken);
            return Ok(script);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("{scriptId:guid}/review")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ScriptReviewResultDto>> ReviewScript(
        Guid contentItemId,
        Guid scriptId,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var result = await scriptService.ReviewScriptAsync(contentItemId, scriptId, email, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("{scriptId:guid}/submit-for-review")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ScriptDto>> SubmitForReview(
        Guid contentItemId,
        Guid scriptId,
        [FromBody] SubmitScriptForReviewRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var script = await scriptService.SubmitForReviewAsync(contentItemId, scriptId, request, email, cancellationToken);
            return Ok(script);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{scriptId:guid}/approve")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ScriptDto>> ApproveScript(
        Guid contentItemId,
        Guid scriptId,
        [FromBody] ApproveScriptRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var script = await scriptService.ApproveScriptAsync(contentItemId, scriptId, request, email, cancellationToken);
            return Ok(script);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{scriptId:guid}/reject")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ScriptDto>> RejectScript(
        Guid contentItemId,
        Guid scriptId,
        [FromBody] RejectScriptRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var script = await scriptService.RejectScriptAsync(contentItemId, scriptId, request, email, cancellationToken);
            return Ok(script);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{scriptId:guid}/reopen")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ScriptDto>> ReopenScript(
        Guid contentItemId,
        Guid scriptId,
        [FromBody] ReopenScriptRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var script = await scriptService.ReopenScriptAsync(contentItemId, scriptId, request, email, cancellationToken);
            return Ok(script);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private string GetCurrentUserEmail() => User.FindFirstValue(ClaimTypes.Email) ?? "anonymous";
}

[ApiController]
[Route("api/v1/content-items/{contentItemId:guid}/storyboard")]
[Route("api/content-items/{contentItemId:guid}/storyboard")]
[Authorize]
public class StoryboardsController(IStoryboardService storyboardService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StoryboardDto>> GetStoryboard(
        Guid contentItemId,
        CancellationToken cancellationToken)
    {
        var storyboard = await storyboardService.GetStoryboardByContentItemIdAsync(contentItemId, cancellationToken);
        if (storyboard == null) return NotFound(new { error = "No storyboard found for this ContentItem." });
        return Ok(storyboard);
    }

    [HttpGet("versions")]
    public async Task<ActionResult<List<StoryboardVersionDto>>> GetStoryboardVersions(
        Guid contentItemId,
        [FromQuery] Guid? storyboardId,
        CancellationToken cancellationToken)
    {
        Guid targetStoryboardId;
        if (storyboardId.HasValue && storyboardId.Value != Guid.Empty)
        {
            targetStoryboardId = storyboardId.Value;
        }
        else
        {
            var storyboard = await storyboardService.GetStoryboardByContentItemIdAsync(contentItemId, cancellationToken);
            if (storyboard == null) return Ok(new List<StoryboardVersionDto>());
            targetStoryboardId = storyboard.Id;
        }

        var versions = await storyboardService.GetStoryboardVersionsAsync(contentItemId, targetStoryboardId, cancellationToken);
        return Ok(versions);
    }

    [HttpGet("versions/{versionId:guid}")]
    public async Task<ActionResult<StoryboardVersionDto>> GetStoryboardVersion(
        Guid contentItemId,
        Guid versionId,
        [FromQuery] Guid? storyboardId,
        CancellationToken cancellationToken)
    {
        Guid targetStoryboardId;
        if (storyboardId.HasValue && storyboardId.Value != Guid.Empty)
        {
            targetStoryboardId = storyboardId.Value;
        }
        else
        {
            var storyboard = await storyboardService.GetStoryboardByContentItemIdAsync(contentItemId, cancellationToken);
            if (storyboard == null) return NotFound(new { error = "Storyboard not found." });
            targetStoryboardId = storyboard.Id;
        }

        var version = await storyboardService.GetStoryboardVersionAsync(contentItemId, targetStoryboardId, versionId, cancellationToken);
        if (version == null) return NotFound(new { error = "Storyboard version not found." });
        return Ok(version);
    }

    [HttpGet("production-eligibility")]
    public async Task<ActionResult<ProductionEligibilityDto>> CheckProductionEligibility(
        Guid contentItemId,
        CancellationToken cancellationToken)
    {
        var result = await storyboardService.CheckProductionEligibilityAsync(contentItemId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<StoryboardDto>> CreateStoryboard(
        Guid contentItemId,
        [FromBody] CreateStoryboardRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var storyboard = await storyboardService.CreateStoryboardAsync(contentItemId, request, email, cancellationToken);
            return CreatedAtAction(nameof(GetStoryboard), new { contentItemId }, storyboard);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{storyboardId:guid}")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<StoryboardDto>> UpdateStoryboard(
        Guid contentItemId,
        Guid storyboardId,
        [FromBody] UpdateStoryboardRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var storyboard = await storyboardService.UpdateStoryboardAsync(contentItemId, storyboardId, request, email, cancellationToken);
            return Ok(storyboard);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("generate")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<StoryboardDto>> GenerateAiStoryboard(
        Guid contentItemId,
        [FromBody] GenerateStoryboardOptions? options,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var storyboard = await storyboardService.GenerateAiStoryboardAsync(contentItemId, options, email, cancellationToken);
            return Ok(storyboard);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("{storyboardId:guid}/review")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<StoryboardReviewResultDto>> ReviewStoryboard(
        Guid contentItemId,
        Guid storyboardId,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var result = await storyboardService.ReviewStoryboardAsync(contentItemId, storyboardId, email, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("{storyboardId:guid}/submit-for-review")]
    [HttpPost("{storyboardId:guid}/submit-review")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<StoryboardDto>> SubmitForReview(
        Guid contentItemId,
        Guid storyboardId,
        [FromBody] SubmitStoryboardForReviewRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var storyboard = await storyboardService.SubmitForReviewAsync(contentItemId, storyboardId, request, email, cancellationToken);
            return Ok(storyboard);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("{storyboardId:guid}/approve")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<StoryboardDto>> ApproveStoryboard(
        Guid contentItemId,
        Guid storyboardId,
        [FromBody] ApproveStoryboardRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var storyboard = await storyboardService.ApproveStoryboardAsync(contentItemId, storyboardId, request, email, cancellationToken);
            return Ok(storyboard);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{storyboardId:guid}/reject")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<StoryboardDto>> RejectStoryboard(
        Guid contentItemId,
        Guid storyboardId,
        [FromBody] RejectStoryboardRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var storyboard = await storyboardService.RejectStoryboardAsync(contentItemId, storyboardId, request, email, cancellationToken);
            return Ok(storyboard);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{storyboardId:guid}/reopen")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<StoryboardDto>> ReopenStoryboard(
        Guid contentItemId,
        Guid storyboardId,
        [FromBody] ReopenStoryboardRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var storyboard = await storyboardService.ReopenStoryboardAsync(contentItemId, storyboardId, request, email, cancellationToken);
            return Ok(storyboard);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{storyboardId:guid}/reconcile")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<StoryboardDto>> ReconcileStoryboard(
        Guid contentItemId,
        Guid storyboardId,
        [FromBody] ReconcileStoryboardRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var storyboard = await storyboardService.ReconcileStoryboardAsync(contentItemId, storyboardId, request, email, cancellationToken);
            return Ok(storyboard);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message,
                currentVersion = ex.CurrentVersion
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    private string GetCurrentUserEmail() => User.FindFirstValue(ClaimTypes.Email) ?? "anonymous";
}

