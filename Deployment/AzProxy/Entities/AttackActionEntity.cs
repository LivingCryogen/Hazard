namespace AzProxy.Entities;

public class AttackActionEntity
{
    public int Id { get; set; } // Auto-increment, primary key (since collected)

    // foreign key for GameSession
    public Guid GameId { get; set; }

    public string SourceTerritory { get; set; } = string.Empty;
    public string TargetTerritory { get; set; } = string.Empty;

    public string PlayerName { get; set; } = string.Empty;
    public string DefenderName { get; set; } = string.Empty;

    public int AttackerInitialArmies { get; set; }
    public int DefenderInitialArmies { get; set; }
    public int AttackerDice { get; set; }
    public int DefenderDice { get; set; }

    public int AttackerLoss { get; set;} = 0;
    public int DefenderLoss { get; set;} = 0;
    public bool Conquered { get; set; } = false;

    public GameSessionEntity GameSession { get; set; } = null!; // Navigation property for EF
}
