namespace AzProxy.DataTransform
{
    public class MoveActionDto
    {
        public int Player { get; set; }
        public string SourceTerritory { get; set; } = string.Empty;
        public string TargetTerritory { get; set;} = string.Empty;
        public bool MaxAdvanced { get; set; }
    }
}
