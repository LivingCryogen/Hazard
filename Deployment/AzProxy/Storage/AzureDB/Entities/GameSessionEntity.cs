namespace AzProxy.Storage.AzureDB.Entities;

public class GameSessionEntity
{
    // Composite Key
    public Guid InstallId { get; set; }
    public Guid GameId { get; set; }

    public bool IsDemo { get; set; } = false;

    // Properties from GameSession model
    public int Version { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }  // Nullable for ongoing games
    public int? Winner { get; set; }        // Nullable for ongoing games
    public string PlayerNames { get; set; } = string.Empty; // CSV of player names

    // Navigation properties
    public ICollection<AttackActionEntity> AttackActions { get; set; } = [];
    public ICollection<MoveActionEntity> MoveActions { get; set; } = [];
    public ICollection<TradeActionEntity> TradeActions { get; set; } = [];
}
