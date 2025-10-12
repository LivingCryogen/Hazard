namespace AzProxy.Entities;

public class TradeActionEntity
{
    // Key
    public Guid GameId { get; set; }
    public int ActionId { get; set; }

    // Foreign composite key to PlayerStatsEntity
    public string PlayerName { get; set; } = string.Empty;
    public Guid InstallID { get; set; }

    // Data
    public bool IsDemo { get; set; } = false;
    public string CardTargets { get; set; } = string.Empty;
    public int TradeValue { get; set; } = 0;
    public int OccupiedBonus { get; set; } = 0;

    public GameSessionEntity GameSession { get; set; } = null!; // Navigation property for EF
}
