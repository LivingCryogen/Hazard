using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model.Stats.StatModels;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Configuration;
using Shared.Services.Serializer;
using System.Text.Json;

namespace Model.Stats.Services;
/// <inheritdoc cref="IStatTracker"/>
public class StatTracker : IStatTracker
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    private GameSession _currentSession;
    

    /// <inheritdoc cref="IStatTracker.TrackedActions"/>
    public int TrackedActions { get => _currentSession.NumActions; }
    /// <inheritdoc cref="IStatTracker.GameID"/>
    public Guid GameID => _currentSession.Id;
    /// <inheritdoc cref="IStatTracker.LastSavePath"/>
    public string? LastSavePath { get; set; }

    /// <summary>
    /// Builds a new <see cref="StatTracker"/> instance for the given game.
    /// </summary>
    /// <param name="game">The current game.</param>
    /// <param name="options">App options for this game.</param>
    /// <param name="loggerFactory">A factory for creating error loggers, provided by DI.</param>
    public StatTracker(IGame game, IOptions<AppConfig> options, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<StatTracker>();
        LastSavePath = game.SavePath;
        _currentSession = new(loggerFactory.CreateLogger<GameSession>(), loggerFactory)
        {
            Version = options.Value.StatVersion,
            Id = game.ID,
            StartTime = DateTime.Now,
            EndTime = null,
            Winner = null
        };

        for (int i = 0; i < game.Players.Count; i++)
        {
            _currentSession.PlayerStats.Add(
                new PlayerStats(loggerFactory.CreateLogger<PlayerStats>())
                {
                    Name = game.Players[i].Name,
                    Number = game.Players[i].Number,
                });
        }
    }
    /// <inheritdoc cref="IStatTracker.RecordAttackAction(TerrID, TerrID, ContID?, int, int, int, int, bool, bool)" />
    public void RecordAttackAction(
        TerrID source,
        TerrID target,
        ContID? conqueredcont,
        int attacker,
        int defender,
        int attackerloss,
        int defenderloss,
        bool retreated,
        bool conquered)
    {
        var attackStats = new GameSession.AttackAction(_loggerFactory.CreateLogger<GameSession.AttackAction>())
        {
            Source = source,
            Target = target,
            ConqueredCont = conqueredcont,
            Attacker = attacker,
            Defender = defender,
            AttackerLoss = attackerloss,
            DefenderLoss = defenderloss,
            Retreated = retreated,
            Conquered = conquered
        };

        _currentSession.Attacks.Add(attackStats);

        foreach (var playerStat in _currentSession.PlayerStats)
            switch (playerStat)
            {
                case { Number: var n } when n == attackStats.Attacker:
                    if (attackStats.AttackerLoss > attackStats.DefenderLoss)
                        playerStat.AttacksLost++;
                    else
                        playerStat.AttacksWon++;

                    if (attackStats.ConqueredCont is ContID)
                        playerStat.ContinentsConquered++;

                    if (attackStats.Conquered)
                        playerStat.Conquests++;

                    if (attackStats.Retreated)
                        playerStat.Retreats++;
                    break;
                case { Number: var n } when n == attackStats.Defender:
                    if (attackStats.Retreated)
                        playerStat.ForcedRetreats++;
                    break;
            }
    }
    /// <inheritdoc cref="IStatTracker.RecordMoveAction(TerrID, TerrID, bool, int)" />
    public void RecordMoveAction(TerrID source, TerrID target, bool maxAdvanced, int player)
    {
        var moveStats = new GameSession.MoveAction(_loggerFactory.CreateLogger<GameSession.MoveAction>())
        {
            Source = source,
            Target = target,
            Player = player,
            MaxAdvanced = maxAdvanced
        };

        var matchedPlayer = _currentSession.PlayerStats.Where(p => p.Number == player);
        if (matchedPlayer is PlayerStats playerStat)
        {
            if (moveStats.MaxAdvanced)
                playerStat.MaxAdvances++;

            playerStat.Moves++;
        }

        _currentSession.Moves.Add(moveStats);
    }
    /// <inheritdoc cref="IStatTracker.RecordTradeAction(List{TerrID}, int, int, int)" />
    public void RecordTradeAction(List<TerrID> cardTargets, int tradeValue, int occupiedBonus, int playerNumber)
    {
        var tradeStats = new GameSession.TradeAction(_loggerFactory.CreateLogger<GameSession.TradeAction>())
        {
            CardTargets = [.. cardTargets],
            TradeValue = tradeValue,
            OccupiedBonus = occupiedBonus
        };

        var player = _currentSession.PlayerStats.Where(p => p.Number == playerNumber);
        if (player is PlayerStats playerStat)
        {
            playerStat.TradeIns++;
            playerStat.TotalOccupationBonus += tradeStats.OccupiedBonus;
        }

        _currentSession.TradeIns.Add(tradeStats);
    }
    public async Task<string> JSONFromGameSession()
    {
        try
        {
            return await Task.Run(() => { return JsonSerializer.Serialize(_currentSession, _jsonOptions); });
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to serialize {session} to JSON: {message}", _currentSession, ex.Message);
            throw;
        }
    }
    /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>/>
    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(async () =>
        {
            List<SerializedData> saveData = [];
            bool hasSavePath = LastSavePath is not null;
            saveData.Add(new SerializedData(typeof(int), hasSavePath ? 1 : 0));
            if (hasSavePath)
                saveData.Add(new SerializedData(typeof(string), LastSavePath!));
            saveData.AddRange(await _currentSession.GetBinarySerials());
            return saveData.ToArray();
        });
    }
    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try
        {
            bool loadSavePath = (int)BinarySerializer.ReadConvertible(reader, typeof(int)) == 1;
            if (loadSavePath)
                LastSavePath = (string)BinarySerializer.ReadConvertible(reader, typeof(string));
            else
                LastSavePath = null;
            GameSession loadedSession = new(_loggerFactory.CreateLogger<GameSession>(), _loggerFactory);
            loadedSession.LoadFromBinary(reader);
            _currentSession = loadedSession;
        }
        catch (Exception ex)
        {
            _logger.LogError("An exception was thrown while loading {StatTracker}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
}