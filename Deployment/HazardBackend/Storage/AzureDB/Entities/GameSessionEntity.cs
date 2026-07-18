namespace HazardBackend.Storage.AzureDB.Entities;

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
    public string? WinnerName { get; set; }  // Nullable for ongoing games

    // Navigation properties
    public ICollection<ClaimActionEntity> ClaimActions { get; set; } = [];
    public ICollection<AttackActionEntity> AttackActions { get; set; } = [];
    public ICollection<MoveActionEntity> MoveActions { get; set; } = [];
    public ICollection<TradeActionEntity> TradeActions { get; set; } = [];
    public ICollection<AcquiredContinentEventEntity> AcquiredContinents { get; set; } = [];
    public ICollection<GameSessionPlayerEntity> GameSessionPlayers { get; set; } = [];
}
