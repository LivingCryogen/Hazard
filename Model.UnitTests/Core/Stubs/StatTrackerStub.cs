using Microsoft.Extensions.Logging;
using Model.Stats.StatModels;
using Model.Tests.Fixtures.Stubs;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;

namespace Model.Tests.Core.Stubs;

public class StatTrackerStub : IStatTracker, IBinarySerializable
{
    private readonly ILogger _logger = new LoggerStubT<StatTrackerStub>();
    private GameSession _currentSession;

    public StatTrackerStub()
    {
        _currentSession = new(new LoggerStubT<GameSession>(), new LoggerFactoryStub())
        {
            Version = 1,
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now,
            EndTime = null,
            Winner = null
        };

        _currentSession.Attacks.Add(new GameSession.AttackAction(new LoggerStubT<GameSession.AttackAction>())
        {
            SourceTerritory = TerrID.SouthernEurope,
            TargetTerritory = TerrID.NorthAfrica,
            Attacker = 0,
            Defender = 1,
            AttackerLoss = 2,
            DefenderLoss = 1,
            Retreated = false,
            Conquered = true
        });

        _currentSession.Moves.Add(new GameSession.MoveAction(new LoggerStubT<GameSession.MoveAction>())
        {
            SourceTerritory = TerrID.NorthAfrica,
            TargetTerritory = TerrID.SouthernEurope,
            MaxAdvanced = true,
            Player = 0
        });

        _currentSession.TradeIns.Add(new GameSession.TradeAction(new LoggerStubT<GameSession.TradeAction>())
        {
            CardTargetTerritories = [TerrID.Alberta, TerrID.Congo, TerrID.Japan],
            TradeValue = 3,
            OccupiedBonus = 0,
        });
    }
    public int TrackedActions { get => _currentSession.NumActions; }

    public Guid GameID => throw new NotImplementedException();

    public string? LastSavePath => throw new NotImplementedException();

    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(async () =>
        {
            List<SerializedData> saveData = [];
            saveData.AddRange(await _currentSession.GetBinarySerials());
            return saveData.ToArray();
        });
    }

    public Task<string> JSONFromGameSession()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try
        {
            GameSession loadedSession = new(new LoggerStubT<GameSession>(), new LoggerFactoryStub());
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
