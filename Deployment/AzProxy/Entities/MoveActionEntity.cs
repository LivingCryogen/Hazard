namespace AzProxy.Entities;

public class MoveActionEntity
{
    public int Id { get; set; } // Auto-increment, primary key (since collected)

    // foreign key for GameSession
    public Guid GameId { get; set; }

    public string PlayerName { get; set; } = string.Empty;

    public string SourceTerritory { get; set; } = string.Empty;
    public string TargetTerritory { get; set; } = string.Empty;
    public bool MaxAdvanced { get; set; } = false;

    public GameSessionEntity GameSession { get; set; } = null!; // Navigation property for EF
}
