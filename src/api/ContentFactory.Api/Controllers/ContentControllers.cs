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
