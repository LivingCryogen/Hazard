using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model.Stats.StatModels;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Configuration;
using Shared.Services.Serializer;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Model.Stats.Services;
/// <inheritdoc cref="IStatTracker"/>
public class StatTracker : IStatTracker
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
        };
    private GameSession _currentSession;
    private int _nextActionId = 1;
    
    /// <inheritdoc cref="IStatTracker.GameID"/>
    public Guid GameID { get => _currentSession.Id; }
    public int TrackedActions { get => _currentSession.NumActions; }

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
        _currentSession = new(loggerFactory.CreateLogger<GameSession>(), loggerFactory)
        {
            Id = game.ID,
            StartTime = DateTime.Now,
            EndTime = null,
            Winner = null
        };

        List<(int, string)> playerNumNameList = [];
        foreach(var player in game.Players)
            playerNumNameList.Add((player.Number, player.Name));
        _currentSession.PlayerNumsAndNames = playerNumNameList.ToDictionary<int, string>();
    }
    /// <inheritdoc cref="IStatTracker.RecordAttackAction(IAttackData)" />
    public void RecordAttackAction(IAttackData attackData)
    {
        var attackStats = new GameSession.AttackAction(_loggerFactory.CreateLogger<GameSession.AttackAction>())
        {
            ActionId = _nextActionId++,
            SourceTerritory = attackData.SourceTerritory,
            TargetTerritory = attackData.TargetTerritory,
            Attacker = attackData.Attacker,
            Defender = attackData.Defender,
            AttackerLoss = attackData.AttackerLoss,
            DefenderLoss = attackData.DefenderLoss,
            Retreated = attackData.Retreated,
            Conquered = attackData.Conquered
        };

        _currentSession.Attacks.Add(attackStats);
    }
    /// <inheritdoc cref="IStatTracker.RecordMoveAction(IMoveData)" />
    public void RecordMoveAction(IMoveData moveData)
    {
        var moveStats = new GameSession.MoveAction(_loggerFactory.CreateLogger<GameSession.MoveAction>())
        {
            ActionId = _nextActionId++,
            SourceTerritory = moveData.SourceTerritory,
            TargetTerritory = moveData.TargetTerritory,
            Player = moveData.Player,
            MaxAdvanced = moveData.MaxAdvanced
        };
        _currentSession.Moves.Add(moveStats);
    }
    /// <inheritdoc cref="IStatTracker.RecordTradeAction(ITradeData)" />
    public void RecordTradeAction(ITradeData tradeData)
    {
        var tradeStats = new GameSession.TradeAction(_loggerFactory.CreateLogger<GameSession.TradeAction>())
        {
            ActionId = _nextActionId++,
            CardTargetTerritories = [.. tradeData.CardTargets],
            TradeValue = tradeData.TradeValue,
            OccupiedBonus = tradeData.OccupiedBonus
        };
        _currentSession.TradeIns.Add(tradeStats);
    }
    /// <inheritdoc cref="IStatTracker.JSONFromGameSession"/>
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
            saveData.Add(new(typeof(int), _nextActionId));
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
            _nextActionId = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
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