namespace AzProxy.Entities;

public class AttackActionEntity
{
    public Guid GameID { get; set; }

    public string SourceTerritory { get; set; } = string.Empty;
    public string TargetTerritory { get; set; } = string.Empty;
    public string? ConqueredContinent { get; set; } = null;

    public string AttackerName { get; set; } = string.Empty;
    public string DefenderName { get; set; } = string.Empty;
    public int AttackerPlayerNumber { get; set; }
    public int DefenderPlayerNumber { get; set; }

    public int AttackerLoss { get; set;} = 0;
    public int DefenderLoss { get; set;} = 0;
}
