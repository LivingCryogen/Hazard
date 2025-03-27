namespace AzProxy;

public class RequestMetrics
{
    public DateTimeOffset UnbannedOn { get; init; } = default;
    public int numBans { get; init; }
}
