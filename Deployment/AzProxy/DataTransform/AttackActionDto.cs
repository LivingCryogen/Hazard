namespace AzProxy.DataTransform;

public class AttackActionDto
{
    public int ActionId { get; set; }
    public int Player { get; set; }
    public string SourceTerritory { get; set; } = string.Empty;
    public string TargetTerritory { get; set; } = string.Empty;
    public int Defender { get; set; }
    public int AttackerInitialArmies { get; set; }
    public int DefenderInitialArmies { get; set; }
    public int AttackerDice { get; set; }
    public int DefenderDice { get; set; }
    public int AttackerLoss { get; set; }
    public int DefenderLoss { get; set; }
    public bool Retreated { get; set; }
    public bool Conquered { get; set; }
}
