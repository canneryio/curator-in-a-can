using Cannery.Conductor.Client.Resolvers;

namespace Cannery.Conductor.Client.Abstractions;

public interface ITagStore
{
    Task<ResolvedTagDto?> TryGetByCanonicalAsync(string canonical, CancellationToken ct);
    Task<ResolvedTagDto> InsertProvisionalAsync(string canonical, IEnumerable<string> aliases, CancellationToken ct);
    Task<ResolvedTagDto> InsertAuthoritativeAsync(Guid curatorId, string canonical, IEnumerable<string> aliases, CancellationToken ct);
    Task<ResolvedTagDto?> GetByIdAsync(Guid tagId, CancellationToken ct);
}
