using Microsoft.Extensions.Logging;
using Model.Tests.Fixtures.Stubs;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Tests.Stats;

public class MockGameSession : IBinarySerializable
{
    private readonly ILogger _logger = new LoggerStubT<MockGameSession>();

    public class AttackAction(LoggerStubT<AttackAction> logger) : IBinarySerializable
    {
        private readonly ILogger _logger = logger;

        public TerrID Source { get; set; }
        public TerrID Target { get; set; }
        public ContID? ConqueredCont { get; set; }
        public int Attacker { get; set; }
        public int Defender { get; set; }
        public int AttackerLoss { get; set; }
        public int DefenderLoss { get; set; }
        public bool Retreated { get; set; }
        public bool Conquered { get; set; }

        public async Task<SerializedData[]> GetBinarySerials()
        {
            return await Task.Run(() =>
            {
                List<SerializedData> saveData = [];
                saveData.Add(new(typeof(TerrID), Source));
                saveData.Add(new(typeof(TerrID), Target));

                if (ConqueredCont is ContID conqueredCont)
                {
                    saveData.Add(new(typeof(int), 1));
                    saveData.Add(new(typeof(ContID), conqueredCont));
                }
                else
                    saveData.Add(new(typeof(int), 0));

                saveData.Add(new(typeof(int), Attacker));
                saveData.Add(new(typeof(int), Defender));
                saveData.Add(new(typeof(int), AttackerLoss));
                saveData.Add(new(typeof(int), DefenderLoss));
                saveData.Add(new(typeof(bool), Retreated));
                saveData.Add(new(typeof(bool), Conquered));
                return saveData.ToArray();
            });
        }
        public bool LoadFromBinary(BinaryReader reader)
        {
            bool loadComplete = true;
            try
            {
                Source = (TerrID)BinarySerializer.ReadConvertible(reader, typeof(TerrID));
                Target = (TerrID)BinarySerializer.ReadConvertible(reader, typeof(TerrID));
                bool hasConqueredCont = (int)BinarySerializer.ReadConvertible(reader, typeof(int)) == 1;
                if (hasConqueredCont)
                    ConqueredCont = (ContID)BinarySerializer.ReadConvertible(reader, typeof(ContID));
                Attacker = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                Defender = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                AttackerLoss = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                DefenderLoss = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                Retreated = (bool)BinarySerializer.ReadConvertible(reader, typeof(bool));
                Conquered = (bool)BinarySerializer.ReadConvertible(reader, typeof(bool));
            }
            catch (Exception ex)
            {
                _logger.LogError("An exception was thrown while loading {AttackAction}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
                loadComplete = false;
            }
            return loadComplete;
        }
    }

    public class MoveAction(LoggerStubT<MoveAction> logger) : IBinarySerializable
    {
        private readonly ILogger _logger = logger;

        public TerrID Source { get; set; }
        public TerrID Target { get; set; }
        public int Player { get; set; }
        public bool MaxAdvanced { get; set; }

        public async Task<SerializedData[]> GetBinarySerials()
        {
            return await Task.Run(() =>
            {
                List<SerializedData> saveData = [];
                saveData.Add(new(typeof(TerrID), Source));
                saveData.Add(new(typeof(TerrID), Target));
                saveData.Add(new(typeof(int), Player));
                saveData.Add(new(typeof(bool), MaxAdvanced));
                return saveData.ToArray();
            });
        }

        public bool LoadFromBinary(BinaryReader reader)
        {
            bool loadComplete = true;
            try
            {
                Source = (TerrID)BinarySerializer.ReadConvertible(reader, typeof(TerrID));
                Target = (TerrID)BinarySerializer.ReadConvertible(reader, typeof(TerrID));
                Player = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                MaxAdvanced = (bool)BinarySerializer.ReadConvertible(reader, typeof(bool));
            }
            catch (Exception ex)
            {
                _logger.LogError("An exception was thrown while loading {MoveAction}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
                loadComplete = false;
            }
            return loadComplete;
        }
    }

    public class TradeAction(LoggerStubT<TradeAction> logger) : IBinarySerializable
    {
        private readonly ILogger _logger = logger;

        public TerrID[] CardTargets { get; set; } = [];
        public int TradeValue { get; set; }
        public int OccupiedBonus { get; set; }

        public async Task<SerializedData[]> GetBinarySerials()
        {
            return await Task.Run(() =>
            {
                List<SerializedData> saveData = [];

                List<IConvertible> convertTargets = [];
                foreach (var target in CardTargets)
                    convertTargets.Add(target);
                saveData.Add(new(typeof(int), convertTargets.Count));
                saveData.Add(new(typeof(TerrID), [.. convertTargets]));

                saveData.Add(new(typeof(int), TradeValue));
                saveData.Add(new(typeof(int), OccupiedBonus));
                return saveData.ToArray();
            });
        }

        public bool LoadFromBinary(BinaryReader reader)
        {
            bool loadComplete = true;
            try
            {
                int numCardTargets = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                CardTargets = (TerrID[])BinarySerializer.ReadConvertibles(reader, typeof(TerrID), numCardTargets);
                TradeValue = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                OccupiedBonus = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            }
            catch (Exception ex)
            {
                _logger.LogError("An exception was thrown while loading {MoveAction}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
                loadComplete = false;
            }
            return loadComplete;
        }
    }

    public int Version { get; set; }
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? Winner { get; set; }
    
    public List<AttackAction> Attacks { get; private set; } = new();
    public List<MoveAction> Moves { get; private set; } = new();
    public List<TradeAction> TradeIns { get; private set; } = new();
    public List<MockPlayerStats> PlayerStats { get; set; } = [];
    public bool LoadFromBinary(BinaryReader reader)
{
    bool loadComplete = true;
    try
    {
        int readVersion = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
        if (Version != readVersion)
        {
            _logger.LogError("Version mismatch! {GameSession} expected version {Number}, but instead tried to load version {readNumber}.", this, Version, readVersion);
            return false;
        }

        Id = Guid.Parse((string)BinarySerializer.ReadConvertible(reader, typeof(string)));
        StartTime = DateTime.Parse((string)BinarySerializer.ReadConvertible(reader, typeof(string)));

        bool hasEndTime = (int)BinarySerializer.ReadConvertible(reader, typeof(int)) == 1;
        if (hasEndTime)
            EndTime = DateTime.Parse((string)BinarySerializer.ReadConvertible(reader, typeof(string)));

        bool hasWinner = (int)BinarySerializer.ReadConvertible(reader, typeof(int)) == 1;
        if (hasWinner)
            Winner = (int)BinarySerializer.ReadConvertible(reader, typeof(int));

        int numAttacks = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
        for (int i = 0; i < numAttacks; i++)
        {
            var readAction = new AttackAction(new LoggerStubT<AttackAction>());
            readAction.LoadFromBinary(reader);
            Attacks.Add(readAction);
        }

        int numMoves = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
        for (int i = 0; i < numMoves; i++)
        {
            var readMove = new MoveAction(new LoggerStubT<MoveAction>());
            readMove.LoadFromBinary(reader);
            Moves.Add(readMove);
        }

        int numTrades = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
        for (int i = 0; i < numTrades; i++)
        {
            var readTrade = new TradeAction(new LoggerStubT<TradeAction>());
            readTrade.LoadFromBinary(reader);
            TradeIns.Add(readTrade);
        }

        int numPlayerStats = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
        for (int i = 0; i < numPlayerStats; i++)
        {
            var readPlayerStat = new MockPlayerStats();
            readPlayerStat.LoadFromBinary(reader);
            PlayerStats.Add(readPlayerStat);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError("An exception was thrown while loading {GameSession}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
        loadComplete = false;
    }
    return loadComplete;
}
    
    public async Task<SerializedData[]> GetBinarySerials()
{
    return await Task.Run(async () =>
    {
        List<SerializedData> saveData = [];
        saveData.Add(new(typeof(int), Version));
        saveData.Add(new(typeof(string), Id.ToString()));
        saveData.Add(new(typeof(string), StartTime.ToString()));

        if (EndTime is DateTime endTime)
        {
            saveData.Add(new(typeof(int), 1));
            saveData.Add(new(typeof(string), endTime.ToString()));
        }
        else
            saveData.Add(new(typeof(int), 0));

        if (Winner is int winner)
        {
            saveData.Add(new(typeof(int), 1));
            saveData.Add(new(typeof(int), winner));
        }
        else
            saveData.Add(new(typeof(int), 0));

        var attackSaveTasks = Attacks.Select(a => a.GetBinarySerials());
        var moveSaveTasks = Moves.Select(m => m.GetBinarySerials());
        var tradeSaveTasks = TradeIns.Select(t => t.GetBinarySerials());
        var playerSaveTasks = PlayerStats.Select(s => s.GetBinarySerials());

        var saveTasks = new[]
        {
            Task.WhenAll(attackSaveTasks),
            Task.WhenAll(moveSaveTasks),
            Task.WhenAll(tradeSaveTasks),
            Task.WhenAll(playerSaveTasks)
        };

        var innerSaveData = await Task.WhenAll(saveTasks);

        int numAttacks = Attacks.Count;
        saveData.Add(new(typeof(int), numAttacks));
        saveData.AddRange(innerSaveData[0].SelectMany(a => a));

        int numMoves = Moves.Count;
        saveData.Add(new(typeof(int), numMoves));
        saveData.AddRange(innerSaveData[1].SelectMany(m => m));

        int numTrades = TradeIns.Count;
        saveData.Add(new(typeof(int), numTrades));
        saveData.AddRange(innerSaveData[2].SelectMany(t => t));

        int numPlayerStats = PlayerStats.Count;
        saveData.Add(new(typeof(int), numPlayerStats));
        saveData.AddRange(innerSaveData[3].SelectMany(p => p));

        return saveData.ToArray();
    });
}
}