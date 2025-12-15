using Azure.Data.Tables;

namespace AzProxy.Storage.AzureTables;

public class AppVarEntry : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; } = default;
    public Azure.ETag ETag { get; set; } = default;
    public string Value { get; set; } = default!;
    public string TypeName { get; set; } = default!;
    public string Description { get; set; } = default!;
}
