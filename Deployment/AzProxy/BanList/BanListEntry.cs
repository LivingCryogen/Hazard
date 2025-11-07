using Azure;
using Azure.Data.Tables;

namespace AzProxy.BanList;

public class BanListEntry : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; } = default;
    public ETag ETag { get; set; } = default;
    public bool NowBanned { get; set; } = default;
    public DateTimeOffset UnbannedOn { get; set; } = default;
    public bool IsLifetime { get; set; } = default;
    public int NumTempBans { get; set; } = default;
}
