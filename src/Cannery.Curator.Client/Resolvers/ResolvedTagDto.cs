using Cannery.Conductor.Client.Entities;

namespace Cannery.Conductor.Client.Resolvers;

public class ResolvedTagDto
{
    public Guid Id { get; set; } = Guid.Empty;
    public string Canonical { get; set; } = string.Empty;
    public IReadOnlyList<string> Aliases { get; set; } = new List<string>();
    public bool IsProvisional { get; set; } = true;
    public double Confidence { get; set; } = 0.0;
    public string Source { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty; // ETag/Rowversion
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public static ResolvedTagDto CreateFrom(TagEntity? entity)
    {
        if (entity is null) return null!;
        return new ResolvedTagDto
        {
            Id = entity.Id,
            Canonical = entity.Canonical,
            Aliases = entity.Aliases,
            IsProvisional = entity.IsProvisional,
            Confidence = entity.Confidence,
            Source = entity.Source,
            CreatedUtc = entity.CreatedUtc
        };
    }
}
