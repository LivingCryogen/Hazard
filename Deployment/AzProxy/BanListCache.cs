using System.Collections.Concurrent;

namespace AzProxy;

public interface IBanCache
{

}

public class BanListCache : IBanCache
{
    public class TableChange
    {
        public bool IsNew { get; init; } = default;
        public bool IsDeleted { get; init; } = default;
        public string? Address { get; init; } = string.Empty;
        public BanListEntry? AffectedEntry { get; init; } = null;
    }

    private readonly ILogger _logger;

    public BanListCache(ILogger logger)
    {
        _logger = logger;
    }

    // Stores banned addresses, their unban date, and the number of previous bans
    public ConcurrentDictionary<string, (DateTimeOffset Unbanned, int BanCount)> TempBanList { get; } = [];
    public List<string> PermaBanList { get; } = [];
    public HashSet<TableChange> PendingChanges { get; } = [];

    public (DateTimeOffset UnBanDate, int NumBans)? this[string address] {
        get {
            try {
                if (PermaBanList.Contains(address))
                    return (DateTimeOffset.MaxValue, 3); // indicates permaban
                if (TempBanList.TryGetValue(address, out (DateTimeOffset, int) value))
                    return value;
                else
                    return null;
            } catch (ArgumentNullException) {
                return null;
            } catch (Exception ex) {
                _logger.LogError(ex, "There was an error when referencing the banlist cache: {message.}", ex.Message);
                return null;
            }
        }
    }

    public async Task<bool> Initialize(HashSet<BanListEntry> banRecords)
    {

    }

    public void TempBan(string address, (string, DateTimeOffset) banInfo)
    {
        try {
            foreach (var entry in newEntries)
                _banListCache.TryAdd(entry.Item1, entry.Item2);
        }
        catch (Exception ex) {
            var errorTime = DateTime.UtcNow;
            _logger.LogError("{DateTime} : There was an error while attempting to update the banlist cache: {message}.",
                errorTime, ex.Message);
            _logger.LogInformation("Error at {time} reported: {data} ; {innerEx} ; {source} ; {trace}",
                errorTime, ex.Data, ex.InnerException, ex.Source, ex.StackTrace);
        }
    }
}
