namespace Cannery.Conductor.Client.Resolvers;

public class TagErrorDto
{
    public string Input { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public static TagErrorDto CreateFrom(TagErrorDto? error)
    {
        if (error is null) return null!;
        return new TagErrorDto
        {
            Input = error.Input,
            Code = error.Code,
            Message = error.Message
        };
    }
}
