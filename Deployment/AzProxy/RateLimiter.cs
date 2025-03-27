using System.Collections.Concurrent;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Net;

namespace AzProxy;

public class RateLimiter(string connectionString, ILogger logger)
{
    // in-memory
    private readonly ConcurrentDictionary<string, (DateTime LastReset, int Count)> _requestCounts = new();
    // Azure Storage Table client for persistence
    private TableClient _tableClient => new TableServiceClient(connectionString).GetTableClient("BannedAddresses");
    private readonly ConcurrentDictionary<string, DateTimeOffset> _banListCache = new();

    private readonly ILogger _logger = logger;


    public async Task<bool> RequestAllowed(string clientIPaddress)
    {
        try {
            if (_banListCache.TryGetValue(clientIPaddress, out ))

            if (!_banListCache.ContainsKey(clientIPaddress)) {
                var banListEntry = await GetBanListEntry(clientIPaddress);
                if (banListEntry is null)
                    return true;
                
                await ReconcileBan(banListEntry);

                await InTableBanList(clientIPaddress, out BanListEntry entry)) {
                    //if (numTempBans >= 2) {
                    //    PermaBanAddress(clientIPaddress);
                    //    _logger.LogInformation("Permanently banned IP {address} because it exceede")
                    //}



                    _banListCache[address] = entry.UnbannedOn;
                    _logger.LogWarning("Blocked request from IP address {address}; the adress is banned until {unbanDate}. Reason: {reason}.",
                        address, entry.UnbannedOn, entry.Reason);

                
            }
        }
    }

    public async Task ReconcileBan(BanListEntry entry)
    {
        if (!entry.NowBanned)
            return;

        if (_banListCache.TryGetValue(entry.RowKey, out var banListEntry)
            && _requestCounts.TryGetValue(entry.RowKey, out (DateTime Last, int Count) requests)
            && requests.Count > 20) {
            if (entry.NumTempBans >= 3)
                await PermaBanAddress(entry.RowKey);
            else
                await TempBanAddress(entry.RowKey);
        }

        if (entry.NumTempBans >= 3) {
            entry.isLifetime = true;
            _banListCache.TryAdd(entry.RowKey, DateTime.MaxValue);
        }

        if (entry.UnbannedOn < DateTimeOffset.UtcNow && !entry.isLifetime) {
            entry.NowBanned = false;
            _banListCache.TryRemove(entry.RowKey, out _);
        }

        await _tableClient.UpdateEntityAsync(entry, entry.ETag);
    }
    public async Task<BanListEntry?> GetBanListEntry(string address)
    {
        try {
            var response = await _tableClient.GetEntityAsync<BanListEntry>("BannedAddress", address);
            return response.Value;
        }
        catch (RequestFailedException failedToFind) when (failedToFind.Status == 404) {
            return null;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error in banlist lookup for IP {address}", address);
            return null;
        }
    }
    private async Task TempBanAddress(string address)
    {
        try {
            var unbanDate = DateTimeOffset.UtcNow.Add(_tempBanLength);
            BanListEntry newEntry = new() {
                PartitionKey = "BannedAddress",
                RowKey = address,
                UnbannedOn = unbanDate,
                Reason = "Exceeded request rate limit.",
                isLifetime = false
            };
            await _tableClient.UpsertEntityAsync(newEntry);
            _banListCache[address] = unbanDate;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error assigning temp ban to IP {address}.", address);
        }
    }
    private async Task PermaBanAddress(string address)
    {

    }
}
