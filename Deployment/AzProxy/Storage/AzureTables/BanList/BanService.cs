using AzProxy.Requests;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AzProxy.Storage.AzureTables.BanList;

public class BanService(ILogger<BanService> logger, IConfiguration config, IBanCache cache)
{
    private readonly ILogger _logger = logger;
    private readonly IBanCache _cache = cache;
    private readonly TimeSpan[] _banSpans = [
        double.TryParse(config["FirstBanMinutes"], out var firstBanMinutes) ?
            TimeSpan.FromMinutes(firstBanMinutes) :
            TimeSpan.FromMinutes(30),
        int.TryParse(config["SecondBanHours"], out var secondBanHours) ?
            TimeSpan.FromHours(secondBanHours) :
            TimeSpan.FromHours(3),
        int.TryParse(config["ThirdBanDays"], out var thirdBanDays) ?
            TimeSpan.FromDays(thirdBanDays) :
            TimeSpan.FromDays(7)
        ];
    private readonly int _maxVerifyRequests =
        int.TryParse(config["MaxVerifyRequests"], out var maxVerify) ?
            maxVerify : 50;
    private readonly int _maxGenSASRequests =
      int.TryParse(config["MaxGenSASRequests"], out var maxGenSAS) ?
          maxGenSAS : 25;
    private readonly int _maxSyncRequests =
      int.TryParse(config["MaxSyncRequests"], out var maxSync) ?
          maxSync : 35;
    private readonly int _maxUnknownRequests =
        int.TryParse(config["MaxUnknownRequests"], out var maxUnknown) ?
            maxUnknown : 10;
    private readonly int _maxLeaderboardRequests =
        int.TryParse(config["MaxLeaderboardRequests"], out var maxLeaderboards) ?
            maxLeaderboards : 50;
    private readonly int _maxSearchRequests =
        int.TryParse(config["MaxSearchRequests"], out var maxSearches) ?
            maxSearches : 35;


    public bool CacheInitialized => _cache.Initialized;

    public bool Allow(string address, RequestType requestType, int requests)
    {
        int maxRequests = requestType switch
        {
            RequestType.Verify => _maxVerifyRequests,
            RequestType.GenSAS => _maxGenSASRequests,
            RequestType.Leaderboard => _maxLeaderboardRequests,
            RequestType.Search => _maxSearchRequests,
            RequestType.Sync => _maxSyncRequests,
            RequestType.None => _maxUnknownRequests,
            _ => 10
        };

        // Check request limit, issue ban and reject if so
        if (requests > maxRequests)
        {
            IssueBan(address);
            return false;
        }
        // If not banned, accept
        if (!_cache.TryGetBan(address, out Ban? ban))
            return true;
        if (ban == null)
        {
            _logger.LogWarning("Address {address} was found in the ban cache but with a null Ban reference.", address);
            return true;
        }
        if (ban.Type == Ban.BanType.None)
        {
            _logger.LogWarning("Address {address} was found in the ban cache with Ban type of none.", address);
            return true;
        }
        if (ban.Type == Ban.BanType.Life)
            return false;
        if (ban.Type == Ban.BanType.Unbanned)
            return true;

        // If tempbanned and ban has not expired, reject; otherwise, update to unbanned and accept
        if (ban.Expiration > DateTime.UtcNow)
            return false;

        SetUnbanned(address, ban);
        return true;
    }

    public void IssueBan(string address)
    {
        _cache.AddOrUpdateBan(address,

            _ => new(Ban.BanType.Temp, DateTime.UtcNow + _banSpans[0]),

            (_, oldBan) => new(
                oldBan.BanCount >= 3 ?
                    Ban.BanType.Life : Ban.BanType.Temp,
                oldBan.BanCount + 1,
                oldBan.BanCount >= 0 && oldBan.BanCount < 3 ?
                    DateTime.UtcNow + _banSpans[oldBan.BanCount] : DateTime.MaxValue)
            );
    }

    public void SetUnbanned(string address, Ban oldBan)
    {
        if (!_cache.TryUpdateBan(address,
            new Ban(Ban.BanType.Unbanned, oldBan.BanCount, DateTime.UtcNow),
            oldBan))
            _logger.LogWarning("Failed to update ban for address {address}.", address);
    }
}
