using System.Collections.Generic;

namespace Cannery.Conductor.Client.Resolvers;

public class TagResolutionResultDto
{
    public IReadOnlyList<ResolvedTagDto> Tags { get; set; } = new List<ResolvedTagDto>();
    public IReadOnlyList<TagErrorDto> Errors { get; set; } = new List<TagErrorDto>();

    public static TagResolutionResultDto CreateFrom(TagResolutionResultDto? result)
    {
        if (result is null) return null!;
        return new TagResolutionResultDto
        {
            Tags = result.Tags,
            Errors = result.Errors
        };
    }
}
