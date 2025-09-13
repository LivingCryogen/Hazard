namespace AzProxy.Entities;

public class GameSessionEntity
{
    // Composite Key
    public Guid InstallId { get; set; }
    public Guid GameId { get; set; } 
   
    // Properties from GameSession model
    public int Version { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }  // Nullable for ongoing games
    public int? Winner { get; set; }        // Nullable for ongoing games

    // Navigation properties (EF will create the relationships)
    public ICollection<GamePlayerEntity> Players { get; set; } = [];
    public ICollection<AttackActionEntity> AttackActions { get; set; } = [];
    public ICollection<MoveActionEntity> MoveActions { get; set; } = [];
    public ICollection<TradeActionEntity> TradeActions { get; set; } = [];
}
