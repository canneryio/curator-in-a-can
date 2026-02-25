using Cannery.Conductor.Client.Resolvers;

namespace Cannery.Conductor.Client.Abstractions;

public interface ITagResolver
{
    Task<TagResolutionResultDto> ResolveAsync(
        IEnumerable<TagInputDto> inputs,
        TagResolutionOptionsDto? options = null,
        CancellationToken ct = default);
}
