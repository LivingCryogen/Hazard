namespace AzProxy;

public class BanService(ILogger logger, IConfiguration config, IBanCache cache)
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
    private readonly int _maxRequests =
        int.TryParse(config["MaxRequests"], out var maxRequests) ? 
            maxRequests : 25;
        
    public bool Allow(string Address, int requests)
    {

    }
}
