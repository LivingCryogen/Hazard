using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats.StatModels;

public record GameSession(
    int Version,
    Guid Id,
    DateTime StartTime,
    DateTime? EndTime,
    int? Winner
)
{
    public record AttackAction(
        TerrID Source,
        TerrID Target,
        ContID? ConqueredCont,
        int Attacker,
        int Defender,
        int AttackerLoss,
        int DefenderLoss,
        bool Retreated,
        bool Conquered
    );

    public record MoveAction(
        TerrID Source,
        TerrID Target,
        int Player,
        bool MaxAdvance
    );

    public record TradeAction(
        List<TerrID> CardTargets,
        int TradeValue,
        bool OccupiedBonus
    );

    public List<AttackAction> Attacks { get; init; } = new();
    public List<MoveAction> Moves { get; init; } = new();
    public List<TradeAction> TradeIns { get; init; } = new();
    public List<PlayerStats> PlayerStats { get; init; } = new();
}
