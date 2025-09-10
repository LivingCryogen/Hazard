namespace AzProxy.Entities;

public class GameSessionEntity
{
    // Composite Key
    public Guid InstallId { get; set; }
    public Guid GameID { get; set; } 
   
    // Properties from GameSession model
    public int Version { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }  // Nullable for ongoing games
    public int? Winner { get; set; }        // Nullable for ongoing games

    // Navigation properties (EF will create the relationships)
    public ICollection<PlayerStatsEntity> PlayerStats { get; set; } = new List<PlayerStatsEntity>();
    public ICollection<AttackActionEntity> AttackActions { get; set; } = new List<AttackActionEntity>();
    public ICollection<MoveActionEntity> MoveActions { get; set; } = new List<MoveActionEntity>();
    public ICollection<TradeActionEntity> TradeActions { get; set; } = new List<TradeActionEntity>();
}
