﻿using Hazard_Share.Enums;
using Hazard_Share.Interfaces.Model;
using System.Collections.ObjectModel;

namespace Hazard_Model.Tests.Core.Mocks;

public class MockRuleValues : IRuleValues
{
    public int MinimumArmyBonus => 1;

    public int TerritoryTradeInBonus => 10;

    public int AttackersLimit => 3;

    public int DefendersLimit => 2;

    public ReadOnlyDictionary<ContID, int> ContinentBonus { get; } = new(new Dictionary<ContID, int>());

    public ReadOnlyDictionary<int, int> SetupActionsPerPlayers { get; } = new(new Dictionary<int, int>());

    public ReadOnlyDictionary<int, int> SetupStartingPool { get; } =
        new(new Dictionary<int, int>([new KeyValuePair<int, int>(1,1), new KeyValuePair<int, int>(2, 2), new KeyValuePair<int, int>(3, 3), new KeyValuePair<int, int>(4, 4),
            new KeyValuePair<int, int>(5, 5), new KeyValuePair<int, int>(6,6)]));

    public int CalculateBaseTradeInBonus(int numTrades)
    {
        throw new NotImplementedException();
    }

    public int CalculateTerritoryBonus(int numTerritories)
    {
        throw new NotImplementedException();
    }
}