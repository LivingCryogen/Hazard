namespace AzProxy;

public record Ban
{
    public enum BanType : int
    {
        Unbanned = -1,
        None = 0,
        Temp = 1,
        Life = 2
    }

    public Ban(BanType type, DateTimeOffset expiration)
    {
        Type = type;
        Expiration = expiration;
        TimeStamp = DateTime.UtcNow;
    }

    public Ban(BanType type, int count, DateTimeOffset expiration)
    {
        Type = type;
        BanCount = count;
        Expiration = expiration;
        TimeStamp = DateTime.UtcNow;
    }

    public BanType Type { get; } = BanType.None;
    public int BanCount { get; } = 1;
    public DateTimeOffset TimeStamp { get; } = default;
    public DateTimeOffset Expiration { get; } = default;
}
