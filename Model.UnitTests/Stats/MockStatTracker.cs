using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Testing.Platform.Logging;
using Model.Stats.StatModels;
using Model.Tests.Fixtures.Mocks;
using Model.Tests.Fixtures.Stubs;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Options;
using Shared.Services.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Tests.Stats;

public class MockStatTracker(IGame<MockTerrID, MockContID> game) : IStatTracker<MockTerrID, MockContID>, IBinarySerializable
{
    private readonly Microsoft.Extensions.Logging.ILogger _logger = new LoggerStubT<MockStatTracker>();
    private MockGameSession _currentSession = new()
        {
            Version = 1,
            Id = game.ID,
            StartTime = DateTime.Now,
            EndTime = null,
            Winner = null,
            PlayerStats = [
                new MockPlayerStats()
                    {
                        Name = game.Players[0].Name,
                        Number = game.Players[0].Number,
                    },
                new MockPlayerStats()
                    {
                        Name = game.Players[1].Name,
                        Number = game.Players[1].Number,
                    },
                ]
        };

    public void RecordAttackAction(
        MockTerrID source,
        MockTerrID target,
        MockContID? conqueredcont,
        int attacker,
        int defender,
        int attackerloss,
        int defenderloss,
        bool retreated,
        bool conquered)
    {
        var attackStats = new MockGameSession.AttackAction(new LoggerStubT<MockGameSession.AttackAction>())
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
    public void RecordMoveAction(MockTerrID source, MockTerrID target, bool maxAdvanced, int player)
    {
        var moveStats = new MockGameSession.MoveAction(new LoggerStubT<MockGameSession.MoveAction>())
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
    public void RecordTradeAction(List<MockTerrID> cardTargets, int tradeValue, int occupiedBonus, int playerNumber)
    {
        var tradeStats = new MockGameSession.TradeAction(new LoggerStubT<MockGameSession.TradeAction>())
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


    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await _currentSession.GetBinarySerials();
    }

    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try
        {
            var readSession = new MockGameSession();
            if (!readSession.LoadFromBinary(reader))
                loadComplete = false;
            _currentSession = readSession;
        }
        catch (Exception ex)
        {
            _logger.LogError("StatTracker encountered an exception when attempting to load from binary: {Message} {InnerEx}", ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
}
