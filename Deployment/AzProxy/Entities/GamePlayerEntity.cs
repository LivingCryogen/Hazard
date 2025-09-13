namespace AzProxy.Entities;

public class GamePlayerEntity
{
    public Guid GameId { get; set; }
    public string Name { get; set; } = string.Empty;

    public GameSessionEntity GameSession { get; set; } = null!; // Navigation property for EF
}
