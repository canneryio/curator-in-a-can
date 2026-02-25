namespace Cannery.Conductor.Client.Resolvers;

public class TagInputDto
{
    public string Value { get; set; } = string.Empty;
    public string? Locale { get; set; }

    public static TagInputDto CreateFrom(TagInputDto? input)
    {
        if (input is null) return null!;
        return new TagInputDto
        {
            Value = input.Value,
            Locale = input.Locale
        };
    }
}
