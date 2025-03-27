using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AzProxy;

public class RequestHandler(ILogger logger, IConfiguration config, BanService banService)
{
    private readonly ILogger _logger = logger;
    private readonly BanService _banService = banService;
    private readonly TimeSpan _requestReset = TimeSpan.FromMinutes(
            double.TryParse(config["RequestResetMinutes"], out double minutes) ? minutes : 15);
            
    private readonly ConcurrentDictionary<string, (DateTime LastReset, int Count)> _requestCounters = new();


    public async Task<bool> ValidateRequest(string iPaddress)
    {
        if (_cache.PermaBanList.Contains(iPaddress))
            return false;

        // Create a request entry, or update one.
        // AddOrUpdate() is needed for Concurrency (it's 'atomic'). 
        var (LastReset, Count) = _requestCounters.AddOrUpdate(iPaddress,
            _ => (DateTime.UtcNow, 1),  // all new entries get this value
            (_, oldValue) => (oldValue.LastReset, oldValue.Count + 1) // updated entries get this value
            );

        // If requester is temporarily banned, and shouldn't yet be unbanned, reject
        if (_cache.TempBanList.TryGetValue(iPaddress, out (DateTimeOffset UnbanDate, int BanCount) banInfo)) {
            
        }
            // If request count exceeds the limit 



            // if requester is in the temp ban list cache, get ban info and then check for request reset
            if (_cache.TempBanList.TryGetValue(iPaddress, out (DateTimeOffset UnbanDate, int BanCount) banInfo)) {
            if (CheckRequestsReset(LastReset, banInfo.BanCount)) {
                _requestCounters[iPaddress] = (DateTime.UtcNow, 1);
                // double check with UnBanDate, then UnBan -- this should hopefully always be true
                if (banInfo.UnbanDate < DateTime.UtcNow) {
                    UnBan(iPaddress);
                    return true;
                }
                else return false;
            }
        }
        else if (CheckRequestsReset(LastReset, 0))
            _requestCounters[iPaddress] = (DateTime.UtcNow, 1);

        // determine if this request breaks the request limit for this requester, if so reject and temp ban
        if (Count > _requestLimit) {
            _cache.TempBan(iPaddress, );
            return false;
        }

        // issue a warning if someone is nearing a ban
        // if (requestsInfo.Count > 22 && requestsInfo.Count < 25)
        // not sure how to do this yet

        return true;
    }

    private void UnBan(string iPaddress) {
        if (_cache.TempBanList.TryRemove(iPaddress, out _))
            _banBuffer.NewUnbanned.Add(iPaddress);
        else
            _logger.LogWarning("Failed to remove IP {address} from the banlist cache.", iPaddress);
    }
    private void TempBan(string iPaddress) {
        if (_cache.TempBanList.TryAdd(iPaddress, )) 
            
        // add to storage buffer
    }
    private bool CheckRequestsReset(DateTime lastReset, int numBans) 
    {
        if (numBans < 0 || numBans > 3)
            return false;
        return (lastReset + _banSpans[numBans]) < DateTime.UtcNow;
    }


}
