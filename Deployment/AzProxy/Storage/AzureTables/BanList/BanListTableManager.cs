using AzProxy.Storage.AzureTables.AppVariables;
using Azure.Data.Tables;

namespace AzProxy.Storage.AzureTables.BanList
{
    internal class BanListTableManager : AzTableManagerBase
    {
        private readonly DateTimeOffset _bootTime = DateTimeOffset.UtcNow;

        public BanListTableManager(ILogger<BanListTableManager> logger, string connectionString, string tableName, TimeSpan entryDuration) 
            : base(new TableClient(connectionString, tableName), logger, tableName, entryDuration) // Note PartitionKey = TableName
        {}

        public ITableEntity GetBanListEntity()
        {
            return new AppVarEntry()
            {
                PartitionKey = PartitionKey,
                Timestamp = DateTime.UtcNow
            };
        }

        // Fetch banlist records from Azure Table storage, applying an optional filter
        public async Task<HashSet<BanListEntry>> GetRecordsAsync(Func<BanListEntry, bool>? filter)
        {
            filter ??= _ => true;

            try
            {
                List<BanListEntry> recordList = [];
                var tableEntities = QueryByPartitionAsync<BanListEntry>(e => e.PartitionKey == PartitionKey);
                await foreach (var tableEntity in tableEntities)
                    recordList.Add(tableEntity);

                var pruneList = recordList.Where(e => ShouldPrune(e));

                var filteredList = recordList
                    .Except(pruneList)
                    .Where(filter);

                _ = Task.Run(() => Prune([.. pruneList]));

                return [.. filteredList];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "There was an error while fetching banlist records: {message}.", ex.Message);
                return [];
            }
        }

        public async Task PersistBan(string address, Ban sessionBan)
        {
            BanListEntry updatedEntry = new()
            {
                PartitionKey = PartitionKey,
                RowKey = address,
                Timestamp = DateTime.UtcNow,
                NowBanned = sessionBan.Type != Ban.BanType.Unbanned,
                UnbannedOn = sessionBan.Expiration,
                IsLifetime = sessionBan.Type == Ban.BanType.Life,
                NumTempBans = sessionBan.BanCount
            };

            if (sessionBan.TimeStamp > _bootTime && sessionBan.BanCount == 1)
                _ = await AddAsync(updatedEntry);
            else
                _ = await UpdateAsync(updatedEntry);
        }

        // Prune a collection of banlist entries from Azure Table storage
        private async Task Prune(BanListEntry[] entries)
        {
            foreach (var entry in entries)
                await DeleteByRowKeyAsync<BanListEntry>(entry.RowKey);
        }

        // Determine if a banlist entry should be pruned based on its age and ban status
        private bool ShouldPrune(BanListEntry entry)
        {
            return entry switch
            {
                { IsLifetime: true } => false,
                { NowBanned: true } when DateTime.UtcNow - entry.Timestamp < EntryDuration => false,
                { NowBanned: true } when DateTime.UtcNow - entry.Timestamp > EntryDuration => true,
                { NowBanned: false } when DateTime.UtcNow - entry.Timestamp > EntryDuration => true,
                _ => false
            };
        }
    }
}
