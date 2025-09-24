using Microsoft.Extensions.Logging;

namespace AzProxy.Entities;

public class PlayerStatsEntity
{
    // Composite Key
    public Guid InstallId { get; set; }
    public string Name { get; set; } = string.Empty;

    public bool IsDemo { get; set; } = false;

    // Aggregate Player Stats
    public int GamesStarted { get; set; } = 0;
    public int GamesCompleted { get; set; } = 0;
    public int GamesWon { get; set; } = 0;
    public DateTime FirstGameStarted { get; set; } = DateTime.MinValue;
    public DateTime? FirstGameCompleted { get; set; }
    public DateTime LastGameStarted { get; set; } = DateTime.MinValue;
    public DateTime? LastGameCompleted { get; set; }
    public TimeSpan TotalGamesDuration { get; set; } = TimeSpan.MinValue;
    public int AttacksWon { get; set; } = 0;
    public int AttacksLost { get; set; } = 0;
    public int AttacksTied { get; set; } = 0;
    public int Conquests { get; set; } = 0;
    public int Retreats { get; set; } = 0;
    public int ForcedRetreats { get; set; } = 0;
    public int AttackDiceRolled { get; set; } = 0;
    public int DefenseDiceRolled { get; set; } = 0;
    public int Moves { get; set; } = 0;
    public int MaxAdvances { get; set; } = 0;
    public int TradeIns { get; set; } = 0;
    public int TotalOccupationBonus { get; set; } = 0;
}
