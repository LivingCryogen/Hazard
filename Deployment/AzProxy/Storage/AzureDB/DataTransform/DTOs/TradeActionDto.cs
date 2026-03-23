namespace HazardBackend.Storage.AzureDB.DataTransform.DTOs;

public record TradeActionDto
{
    public int ActionId { get; set; }
    public int Player { get; set; }
    public string[] CardTargets { get; set; } = [];
    public int TradeValue { get; set; }
    public int OccupiedBonus { get; set; }
}
