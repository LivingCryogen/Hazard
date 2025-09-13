namespace AzProxy.DataTransform;

public class PlayerStatsDto
{
    public string Name { get; set; } = string.Empty;
    public int Number { get; set; } = -2; // -1 for AI, 0-5 for players
    public int AttacksWon { get; set; } = 0;
    public int AttacksLost { get; set; } = 0;
    public int Conquests { get; set; } = 0;
    public int Retreats { get; set; } = 0;
    public int ForcedRetreats { get; set; } = 0;
    public int Moves { get; set; } = 0;
    public int MaxAdvances { get; set; } = 0;
    public int TradeIns { get; set; } = 0;
    public int TotalOccupationBonus { get; set; } = 0;
}
