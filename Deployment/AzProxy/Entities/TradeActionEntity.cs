namespace AzProxy.Entities;

public class TradeActionEntity
{
    public int Id { get; set; } // Auto-increment, primary key (since collected)

    // foreign key for GameSession
    public Guid GameId { get; set; }

    public string PlayerName { get; set; } = string.Empty;

    public string CardTargets { get; set; } = string.Empty;
    public int TradeValue { get; set; } = 0;
    public int OccupiedBonus = 0;

    public GameSessionEntity GameSession { get; set; } = null!; // Navigation property for EF
}
