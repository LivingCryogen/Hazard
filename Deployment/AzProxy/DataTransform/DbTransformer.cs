using AzProxy.Context;
using AzProxy.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AzProxy.DataTransform;

public class DbTransformer(GameStatsDbContext context)
{
    private readonly GameStatsDbContext _context = context;

    public void TransformFromJson(string json, string installID, int trackedActions)
    {
        var sessionData = JsonSerializer.Deserialize<GameSe>(json);

    }

    public GameSessionEntity
}
