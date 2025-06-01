using System.Collections.Concurrent;

namespace AzProxy;

public interface IBanCache
{
    bool TryGetBan(string address, out Ban? ban);
    bool TryUpdateBan(string address, Ban newBan, Ban oldBan);
    void AddOrUpdateBan(string address, Func<string, Ban> addFactory, Func<string, Ban, Ban> updateFactory);
    IEnumerable<string> GetUpdatedAddresses();
    public void Initialize(HashSet<BanListEntry> banRecords);
    public bool Initialized { get; }
}

public class BanListCache(ILogger<BanListCache> logger) : IBanCache
{
    private readonly ILogger _logger = logger;
    private readonly ConcurrentDictionary<string, Ban> _bans = [];
    private readonly HashSet<string> _addressesUpdated = [];
    private readonly object _lock = new();

    public bool Initialized { get; private set; } = false;

    public bool TryGetBan(string address, out Ban? ban)
    {
        if (_bans.TryGetValue(address, out Ban? value) && value != null)
        {
            ban = value;
            return true;
        }
        ban = value;
        return false;
    }

    public void AddOrUpdateBan(string address, Func<string, Ban> addFactory, Func<string, Ban, Ban> updateFactory)
    {
        _bans.AddOrUpdate(address, addFactory, updateFactory);

        lock (_lock)
        {
            _addressesUpdated.Add(address);
        }
    }

    public bool TryUpdateBan(string address, Ban newBan, Ban oldBan)
    {
        if (_bans.TryUpdate(address, newBan, oldBan))
        {
            lock (_lock)
            {
                _addressesUpdated.Add(address);
            }
            return true;
        }
        return false;
    }

    public IEnumerable<string> GetUpdatedAddresses()
    {
        lock (_lock)
        {
            return [.. _addressesUpdated];
        }
    }

    public void Initialize(HashSet<BanListEntry> banRecords)
    {
        foreach (BanListEntry entry in banRecords)
        {
            if (!entry.NowBanned)
                continue;

            string address = entry.RowKey;
            Ban cachedBan = new(
                entry.IsLifetime ? Ban.BanType.Life : Ban.BanType.Temp,
                entry.NumTempBans,
                entry.UnbannedOn
                );

            _bans.AddOrUpdate(address, _ => cachedBan, (_, _) => cachedBan);
        }

        Initialized = true;
    }
}
