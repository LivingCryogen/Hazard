namespace AzProxy.Entities;

public class GamePlayerEntity
{
    // Key
    public Guid GameId { get; set; }
    public int PlayerNumber { get; set; }

    // Data
    public string Name { get; set; } = string.Empty;
    public bool IsDemo { get; set; } = false;

    public GameSessionEntity GameSession { get; set; } = null!; // Navigation property for EF
}
