namespace AzProxy.DataTransform
{
    public class TradeActionDto
    {
        public int Player { get; set ; }
        public string[] CardTargets { get; set; } = [];
        public int TradeValue { get; set; }
        public int OccupiedBonus { get; set; }
    }
}
