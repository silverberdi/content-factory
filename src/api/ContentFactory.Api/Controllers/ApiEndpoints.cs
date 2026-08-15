using System.Security.Claims;
using ContentFactory.Api.Modules.Audit;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Content;
using ContentFactory.Api.Modules.Dashboard;
using ContentFactory.Api.Modules.Discovery;
using ContentFactory.Api.Modules.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentFactory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await dashboardService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChannelsController(IChannelService channelService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ChannelDto>>> GetAll(CancellationToken cancellationToken)
    {
        var channels = await channelService.GetAllChannelsAsync(cancellationToken);
        return Ok(channels);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChannelDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var channel = await channelService.GetChannelByIdAsync(id, cancellationToken);
        if (channel == null) return NotFound();
        return Ok(channel);
    }

    [HttpPost]
    [Authorize(Policy = "RequireChannelManage")]
    public async Task<ActionResult<ChannelDto>> Create([FromBody] CreateChannelRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = GetCurrentUserEmail();

        try
        {
            var channel = await channelService.CreateChannelAsync(request, userId, email, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = channel.Id }, channel);
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

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireChannelManage")]
    public async Task<ActionResult<ChannelDto>> Update(Guid id, [FromBody] UpdateChannelRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = GetCurrentUserEmail();

        try
        {
            var channel = await channelService.UpdateChannelAsync(id, request, userId, email, cancellationToken);
            return Ok(channel);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireChannelManage")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = GetCurrentUserEmail();

        try
        {
            await channelService.DeleteChannelAsync(id, userId, email, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }

    private string GetCurrentUserEmail() => User.FindFirstValue(ClaimTypes.Email) ?? "anonymous";
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IdentityController(IIdentityService identityService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? "silverio.bernal@gmail.com";
        var user = await identityService.GetCurrentUserAsync(email, cancellationToken);
        return Ok(user);
    }

    [HttpGet("users")]
    [Authorize(Policy = "RequireTechnical")]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers(CancellationToken cancellationToken)
    {
        var users = await identityService.GetAllUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("invitations")]
    [Authorize(Policy = "RequireTechnical")]
    public async Task<ActionResult<List<UserInvitationDto>>> GetPendingInvitations(CancellationToken cancellationToken)
    {
        var invitations = await identityService.GetPendingInvitationsAsync(cancellationToken);
        return Ok(invitations);
    }

    [HttpPost("invitations")]
    [Authorize(Policy = "RequireUsersInvite")]
    public async Task<ActionResult<UserInvitationDto>> CreateInvitation([FromBody] InviteUserRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = GetCurrentUserEmail();

        try
        {
            var invitation = await identityService.CreateInvitationAsync(request, userId, email, cancellationToken);
            return Ok(invitation);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("invitations/{id:guid}")]
    [Authorize(Policy = "RequireTechnical")]
    public async Task<IActionResult> RevokeInvitation(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = GetCurrentUserEmail();

        await identityService.RevokeInvitationAsync(id, userId, email, cancellationToken);
        return NoContent();
    }

    [HttpPut("users/{id:guid}/roles")]
    [Authorize(Policy = "RequireUsersRolesManage")]
    public async Task<IActionResult> UpdateRoles(Guid id, [FromBody] UpdateUserRolesRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = GetCurrentUserEmail();

        try
        {
            await identityService.UpdateUserRolesAsync(id, request.Roles, userId, email, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("users/{id:guid}/status")]
    [Authorize(Policy = "RequireTechnical")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] bool isActive, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = GetCurrentUserEmail();

        try
        {
            await identityService.SetUserActiveStatusAsync(id, isActive, userId, email, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("users/{id:guid}")]
    [Authorize(Policy = "RequireTechnical")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = GetCurrentUserEmail();

        try
        {
            await identityService.DeleteUserAsync(id, userId, email, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }

    private string GetCurrentUserEmail() => User.FindFirstValue(ClaimTypes.Email) ?? "anonymous";
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireTechnical")]
public class AuditController(IAuditService auditService) : ControllerBase
{
    [HttpGet("recent")]
    public async Task<ActionResult<List<AuditEventDto>>> GetRecent([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var events = await auditService.GetRecentEventsAsync(limit, cancellationToken);
        return Ok(events);
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DiscoveryController(
    IDiscoveryService discoveryService,
    IContentService contentService) : ControllerBase
{
    [HttpGet("sources")]
    [Authorize(Policy = "RequireDiscoveryManage")]
    public async Task<ActionResult<List<DiscoverySourceDto>>> GetSources([FromQuery] Guid? channelId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var sources = await discoveryService.GetSourcesAsync(channelId, status, cancellationToken);
        return Ok(sources);
    }

    [HttpGet("sources/{id:guid}")]
    [Authorize(Policy = "RequireDiscoveryManage")]
    public async Task<ActionResult<DiscoverySourceDto>> GetSourceById(Guid id, CancellationToken cancellationToken)
    {
        var source = await discoveryService.GetSourceByIdAsync(id, cancellationToken);
        if (source == null) return NotFound();
        return Ok(source);
    }

    [HttpPost("sources")]
    [Authorize(Policy = "RequireDiscoveryManage")]
    public async Task<ActionResult<DiscoverySourceDto>> CreateSource([FromBody] CreateDiscoverySourceRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = GetCurrentUserEmail();

        try
        {
            var source = await discoveryService.CreateSourceAsync(request, userId, email, cancellationToken);
            return CreatedAtAction(nameof(GetSourceById), new { id = source.Id }, source);
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

    [HttpPut("sources/{id:guid}")]
    [Authorize(Policy = "RequireDiscoveryManage")]
    public async Task<ActionResult<DiscoverySourceDto>> UpdateSource(Guid id, [FromBody] UpdateDiscoverySourceRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = GetCurrentUserEmail();

        try
        {
            var source = await discoveryService.UpdateSourceAsync(id, request, userId, email, cancellationToken);
            return Ok(source);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete("sources/{id:guid}")]
    [Authorize(Policy = "RequireTechnical")]
    public async Task<IActionResult> DeleteSource(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = GetCurrentUserEmail();

        try
        {
            await discoveryService.DeleteSourceAsync(id, userId, email, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("sources/{id:guid}/sync")]
    [Authorize(Policy = "RequireDiscoveryManage")]
    public async Task<ActionResult<object>> SyncSource(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = GetCurrentUserEmail();

        try
        {
            var newItemsCount = await discoveryService.SyncSourceAsync(id, userId, email, cancellationToken);
            return Ok(new { synced = true, newItemsCount });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("candidates")]
    [Authorize(Policy = "RequireDiscoveryManage")]
    public async Task<ActionResult<List<DiscoveryCandidateDto>>> GetCandidates(
        [FromQuery] Guid? channelId,
        [FromQuery] string? status,
        [FromQuery] Guid? sourceId,
        [FromQuery] string? search,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var candidates = await discoveryService.GetCandidatesAsync(channelId, status, sourceId, search, limit, cancellationToken);
        return Ok(candidates);
    }

    [HttpGet("candidates/{id:guid}")]
    [Authorize(Policy = "RequireDiscoveryManage")]
    public async Task<ActionResult<DiscoveryCandidateDto>> GetCandidateById(Guid id, CancellationToken cancellationToken)
    {
        var candidate = await discoveryService.GetCandidateByIdAsync(id, cancellationToken);
        if (candidate == null) return NotFound();
        return Ok(candidate);
    }

    [HttpPost("candidates/manual")]
    [Authorize(Policy = "RequireDiscoveryManage")]
    public async Task<ActionResult<DiscoveryCandidateDto>> QuickSubmitCandidate([FromBody] QuickSubmitCandidateRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = GetCurrentUserEmail();

        try
        {
            var candidate = await discoveryService.QuickSubmitCandidateAsync(request, userId, email, cancellationToken);
            return CreatedAtAction(nameof(GetCandidateById), new { id = candidate.Id }, candidate);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("candidates/{id:guid}/triage")]
    [Authorize(Policy = "RequireDiscoveryManage")]
    public async Task<ActionResult<DiscoveryCandidateDto>> TriageCandidate(Guid id, [FromBody] TriageCandidateRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var email = GetCurrentUserEmail();

        try
        {
            var candidate = await discoveryService.TriageCandidateAsync(id, request, userId, email, cancellationToken);
            return Ok(candidate);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("candidates/{id:guid}/initiate-content")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ContentItemDto>> InitiateContentFromCandidate(
        Guid id,
        [FromBody] InitiateContentFromCandidateRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var item = await contentService.InitiateContentFromCandidateAsync(id, request, email, cancellationToken);
            return Ok(item);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("candidates/{id:guid}/attach-to-content")]
    [Authorize(Policy = "RequireEditorial")]
    public async Task<ActionResult<ContentItemEvidenceDto>> AttachCandidateToContent(
        Guid id,
        [FromBody] AttachCandidateToContentRequest request,
        CancellationToken cancellationToken)
    {
        var email = GetCurrentUserEmail();
        try
        {
            var evidence = await contentService.AttachCandidateToContentAsync(id, request, email, cancellationToken);
            return Ok(evidence);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("summary")]
    [Authorize(Policy = "RequireDiscoveryManage")]
    public async Task<ActionResult<DiscoverySummaryDto>> GetSummary([FromQuery] Guid? channelId, CancellationToken cancellationToken)
    {
        var summary = await discoveryService.GetSummaryAsync(channelId, cancellationToken);
        return Ok(summary);
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }

    private string GetCurrentUserEmail() => User.FindFirstValue(ClaimTypes.Email) ?? "anonymous";
}


