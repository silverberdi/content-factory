using System.Text.Json;
using System.Text.RegularExpressions;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Audit;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Modules.Content;

public interface IScriptService
{
    Task<ScriptDto?> GetScriptByContentItemIdAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default);

    Task<ScriptDto?> GetScriptByIdAsync(
        Guid contentItemId,
        Guid scriptId,
        CancellationToken cancellationToken = default);

    Task<List<ScriptVersionDto>> GetScriptVersionsAsync(
        Guid contentItemId,
        Guid scriptId,
        CancellationToken cancellationToken = default);

    Task<ScriptVersionDto?> GetScriptVersionAsync(
        Guid contentItemId,
        Guid scriptId,
        Guid versionId,
        CancellationToken cancellationToken = default);

    Task<ScriptDto> CreateScriptAsync(
        Guid contentItemId,
        CreateScriptRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ScriptDto> UpdateScriptAsync(
        Guid contentItemId,
        Guid scriptId,
        UpdateScriptRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ScriptDto> GenerateAiScriptAsync(
        Guid contentItemId,
        GenerateScriptOptions? options,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ScriptReviewResultDto> ReviewScriptAsync(
        Guid contentItemId,
        Guid scriptId,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ScriptDto> SubmitForReviewAsync(
        Guid contentItemId,
        Guid scriptId,
        SubmitScriptForReviewRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ScriptDto> ApproveScriptAsync(
        Guid contentItemId,
        Guid scriptId,
        ApproveScriptRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ScriptDto> RejectScriptAsync(
        Guid contentItemId,
        Guid scriptId,
        RejectScriptRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<ScriptDto> ReopenScriptAsync(
        Guid contentItemId,
        Guid scriptId,
        ReopenScriptRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);
}
