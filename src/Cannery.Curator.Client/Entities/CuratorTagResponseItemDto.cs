namespace Cannery.Conductor.Client.Entities;

public class CuratorTagResponseItemDto
{
    public Guid CuratorTagId { get; set; }
    public string Canonical { get; set; } = string.Empty;
    public string[] Aliases { get; set; } = [];
}
