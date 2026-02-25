namespace Cannery.Conductor.Client.Resolvers;

public class TagResolutionOptionsDto
{
    public TagResolutionMode Mode { get; set; } = TagResolutionMode.Provisional;
    public bool AllowCreate { get; set; } = true;
    public double MinConfidence { get; set; } = 0.60;

    public static TagResolutionOptionsDto CreateFrom(TagResolutionOptionsDto? options)
    {
        if (options is null) return null!;
        return new TagResolutionOptionsDto
        {
            Mode = options.Mode,
            AllowCreate = options.AllowCreate,
            MinConfidence = options.MinConfidence
        };
    }
}
