namespace AzProxy.Entities;

public class MoveActionEntity
{
    // Key
    public Guid GameId { get; set; }
    public int ActionId { get; set; }

    // Data
    public string PlayerName { get; set; } = string.Empty;
    public bool IsDemo { get; set; } = false;
    public string SourceTerritory { get; set; } = string.Empty;
    public string TargetTerritory { get; set; } = string.Empty;
    public bool MaxAdvanced { get; set; } = false;

    public GameSessionEntity GameSession { get; set; } = null!; // Navigation property for EF
}
