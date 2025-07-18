using Shared.Geography.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Interfaces.Model;

public interface IStatTracker
{
    public void RecordAttackAction(TerrID source,
        TerrID target,
        ContID? conqueredcont,
        int attacker,
        int defender,
        int attackerloss,
        int defenderloss,
        bool retreated,
        bool conquered);

    public void RecordMoveAction(TerrID source,
        TerrID target,
        bool maxAdvanced,
        int player);

    public void RecordTradeAction(List<TerrID> cardTargets,
        int tradeValue,
        int occupiedBonus,
        int playerNumber);
}
