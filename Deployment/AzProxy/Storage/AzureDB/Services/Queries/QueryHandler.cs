using HazardBackend.Storage.AzureDB.Context;
using System.Text.Json.Nodes;
using static System.Formats.Asn1.AsnWriter;

namespace HazardBackend.Storage.AzureDB.Services.Queries;

public class QueryHandler(GameStatsDbContext dbContext)
{
    public async Task<QueryResult> HandleQueryAsync(string queryType, string[] queryParams)
    {
        
    }

            //    using var scope = _serviceProvider.CreateScope();
            //var dbContext = scope.ServiceProvider.GetRequiredService<GameStatsDbContext>();
}
