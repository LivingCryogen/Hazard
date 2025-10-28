using Microsoft.Extensions.Logging;
using Model.Stats.StatModels;
using Model.Tests.Fixtures.Stubs;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Helpers;
using Shared.Services.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Model.Tests.Core.Mocks;

public class MockStatTracker : IStatTracker
{
    private ILogger _logger = LoggerFactoryStub.CreateLogger<MockStatTracker>();
    private ILoggerFactory _loggerFactory = new LoggerFactoryStub();
    private int _actionId = 0;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private GameSession? _currentSession;

    public MockStatTracker()
    {
        _currentSession = null;
    }

    public MockStatTracker(IGame? mockGame, Guid installID)
    {
        if (mockGame == null)
        {
            _currentSession = null;
            return;
        }

        _currentSession = new GameSession(LoggerFactoryStub.CreateLogger<GameSession>(), _loggerFactory) 
        {
            Id = mockGame.ID,
            InstallId = installID,
            PlayerNumsAndNames = mockGame.Players.ToDictionary(p => p.Number, p => p.Name),
            StartTime = UtcDateTimeFormatter.Normalize(DateTime.UtcNow - TimeSpan.FromDays(3)),
            EndTime = UtcDateTimeFormatter.Normalize(DateTime.UtcNow),
            Winner = 0,
        };

        _currentSession.Attacks.Add(new GameSession.AttackAction(LoggerFactoryStub.CreateLogger<GameSession.AttackAction>())
        {
            ActionId = 0,
            Attacker = 0,
            Defender = 1,
            SourceTerritory = TerrID.Alaska,
            TargetTerritory = TerrID.NorthwestTerritory,
            AttackerInitialArmies = 5,
            DefenderInitialArmies = 3,
            AttackerDice = 3,
            DefenderDice = 2,
            AttackerLoss = 1,
            DefenderLoss = 1,
            Conquered = false,
            Retreated = false,
        });

        _currentSession.Attacks.Add(new GameSession.AttackAction(LoggerFactoryStub.CreateLogger<GameSession.AttackAction>())
        {
            ActionId = 1,
            Attacker = 0,
            Defender = 1,
            SourceTerritory = TerrID.Alaska,
            TargetTerritory = TerrID.NorthwestTerritory,
            AttackerInitialArmies = 4,
            DefenderInitialArmies = 2,
            AttackerDice = 3,
            DefenderDice = 2,
            AttackerLoss = 0,
            DefenderLoss = 2,
            Conquered = true,
            Retreated = false,
        });

        _currentSession.Moves.Add(new GameSession.MoveAction(LoggerFactoryStub.CreateLogger<GameSession.MoveAction>())
        {
            ActionId = 2,
            Player = 0,
            SourceTerritory = TerrID.Alaska,
            TargetTerritory = TerrID.NorthwestTerritory,
            MaxAdvanced = true,
        });

        _currentSession.TradeIns.Add(new GameSession.TradeAction(LoggerFactoryStub.CreateLogger<GameSession.TradeAction>())
        {
            Player = 1,
            ActionId = 3,
            CardTargetTerritories = [TerrID.Brazil, TerrID.Egypt, TerrID.Ukraine],
            OccupiedBonus = 0,
            TradeValue = 4,
        });

        _actionId = 3;
    }

    public int TrackedActions => _currentSession?.NumActions ?? 0;

    public Guid GameID => _currentSession?.Id ?? Guid.Empty;

    public bool Completed => _currentSession?.EndTime.HasValue ?? false;

    public GameSession? CurrentSession => _currentSession;

    public void CompleteGame(int winningPlayerNumber)
    {
        throw new NotImplementedException();
    }

    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(async () =>
        {
            List<SerializedData> saveData = [];
            saveData.Add(new(typeof(int), _actionId));
            if (_currentSession != null)
                saveData.AddRange(await _currentSession.GetBinarySerials());
            return saveData.ToArray();
        });
    }

    public async Task<string> JSONFromGameSession()
    {
        if (_currentSession == null)
            return string.Empty;

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

    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try
        {
            _actionId = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            GameSession loadedSession = new(LoggerFactoryStub.CreateLogger<GameSession>(), _loggerFactory);
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

    public void RecordAttackAction(IAttackData attackData)
    {
        throw new NotImplementedException();
    }

    public void RecordMoveAction(IMoveData moveData)
    {
        throw new NotImplementedException();
    }

    public void RecordTradeAction(ITradeData tradeData)
    {
        throw new NotImplementedException();
    }
}
