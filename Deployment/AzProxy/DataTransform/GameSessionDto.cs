namespace AzProxy.DataTransform;

public class GameSessionDto
{
    public int Version { get; set; }
    public Guid Id { get; set; }
    public Guid InstallId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? Winner { get; set; }
    public int NumActions { get; set; }
    public List<AttackActionDto> Attacks { get; set; } = [];
    public List<MoveActionDto> Moves { get; set; } = [];
    public List<TradeActionDto> Trades { get; set; } = [];
    public Dictionary<string, string> PlayerNumsAndNames { get; set; } = [];
}
