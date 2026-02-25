namespace Cannery.Conductor.Client.Entities;

public class CuratorTagRequestDto
{
    public string Canonical { get; set; } = string.Empty;
    public string[] Aliases { get; set; } = [];
    public string Source { get; set; } = string.Empty;
    public string? Locale { get; set; }
    public Guid ProvisionalId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
