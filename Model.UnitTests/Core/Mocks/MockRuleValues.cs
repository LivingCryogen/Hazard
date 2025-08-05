using Model.Tests.Fixtures.Mocks;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using System.Collections.ObjectModel;

namespace Model.Tests.Core.Mocks;

public class MockRuleValues : IRuleValues<MockContID>
{
    public int MinimumArmyBonus => 1;

    public int TerritoryTradeInBonus => 10;

    public int AttackersLimit => 3;

    public int DefendersLimit => 2;

    public ReadOnlyDictionary<MockContID, int> ContinentBonus { get; } = new(new Dictionary<MockContID, int>());

    public ReadOnlyDictionary<int, int> SetupActionsPerPlayers { get; } =
        new(new Dictionary<int, int>([new(0, 13), new(1, 14), new(2, 15), new(3, 16), new(4, 17), new(5, 18), new(6, 19)]));

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
