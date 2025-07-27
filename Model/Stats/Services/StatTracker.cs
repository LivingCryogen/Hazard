using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model.Core;
using Model.Stats.StatModels;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Options;
using Shared.Services.Serializer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats.Services;

public class StatTracker: IStatTracker, IBinarySerializable
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private GameSession _currentSession;
    /// <summary>
    /// Builds a new <see cref="StatTracker"/> instance for the given game.
    /// </summary>
    /// <param name="game">The current game.</param>
    /// <param name="options">App options for this game.</param>
    /// <param name="loggerFactory">A factory for creating error loggers, provided by DI.</param>
    public StatTracker(IGame<TerrID, ContID> game, IOptions<AppConfig> options, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<StatTracker>();

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
    /// <inheritdoc cref="IStatTracker{T, U}.RecordMoveAction(T, T, bool, int)" />
    public void RecordMoveAction(TerrID source, TerrID target, bool maxAdvanced, int player) {
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
    /// <inheritdoc cref="IStatTracker{T, U}.RecordTradeAction(List{T}, int, int, int)" />
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
    /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>/>
    public async Task<SerializedData[]> GetBinarySerials()
    {
        throw new NotImplementedException();
    }
    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
    public bool LoadFromBinary(BinaryReader reader)
    {
        throw new NotImplementedException();
    }
}
