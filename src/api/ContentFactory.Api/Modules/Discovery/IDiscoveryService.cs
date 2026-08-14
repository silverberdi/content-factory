namespace ContentFactory.Api.Modules.Discovery;

public interface IDiscoveryService
{
    Task<List<DiscoverySourceDto>> GetSourcesAsync(Guid? channelId = null, string? status = null, CancellationToken cancellationToken = default);
    Task<DiscoverySourceDto?> GetSourceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DiscoverySourceDto> CreateSourceAsync(CreateDiscoverySourceRequest request, Guid actorId, string actorEmail, CancellationToken cancellationToken = default);
    Task<DiscoverySourceDto> UpdateSourceAsync(Guid id, UpdateDiscoverySourceRequest request, Guid actorId, string actorEmail, CancellationToken cancellationToken = default);
    Task DeleteSourceAsync(Guid id, Guid actorId, string actorEmail, CancellationToken cancellationToken = default);
    Task<int> SyncSourceAsync(Guid id, Guid actorId, string actorEmail, CancellationToken cancellationToken = default);
    
    Task<List<DiscoveryCandidateDto>> GetCandidatesAsync(Guid? channelId = null, string? status = null, Guid? sourceId = null, string? search = null, int limit = 100, CancellationToken cancellationToken = default);
    Task<DiscoveryCandidateDto?> GetCandidateByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DiscoveryCandidateDto> QuickSubmitCandidateAsync(QuickSubmitCandidateRequest request, Guid actorId, string actorEmail, CancellationToken cancellationToken = default);
    Task<DiscoveryCandidateDto> TriageCandidateAsync(Guid id, TriageCandidateRequest request, Guid actorId, string actorEmail, CancellationToken cancellationToken = default);
    Task<DiscoverySummaryDto> GetSummaryAsync(Guid? channelId = null, CancellationToken cancellationToken = default);
}
