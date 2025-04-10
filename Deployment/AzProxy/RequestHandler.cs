using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AzProxy;

public class RequestHandler(ILogger<RequestHandler> logger, IConfiguration config, BanService banService)
{
    private readonly ILogger _logger = logger;
    private readonly BanService _banService = banService;
    private readonly TimeSpan _requestReset = TimeSpan.FromMinutes(
            double.TryParse(config["RequestResetMinutes"], out double minutes) ? minutes : 15);
            
    private readonly ConcurrentDictionary<string, (DateTime LastReset, int Count)> _requestCounters = new();


    public bool ValidateRequest(string iPaddress)
    {
        if (!_banService.CacheInitialized) {
            int timeOut = 10000;
            int checkInterval = 100;
            int timeSpent = 0;
        
            while (!_banService.CacheInitialized) {
                Thread.Sleep(checkInterval);
                timeSpent += checkInterval;
        
                if (timeSpent >  timeOut) {
                    _logger.LogError("Cache initialization timed out.");
                    return false;
                }
            }
        }
        
        // Create a request entry, or update one using concurrent-safe method
        var (LastReset, Count) = _requestCounters.AddOrUpdate(iPaddress,
            _ => (DateTime.UtcNow, 1),  // all new entries get this value
            (_, oldValue) => // old entries reset or updated
                (DateTime.UtcNow - oldValue.LastReset > _requestReset) ? (DateTime.UtcNow, 1) : (oldValue.LastReset, oldValue.Count + 1));
        
        if (!_banService.Allow(iPaddress, Count))
            return false;

        return true;
    }
}
