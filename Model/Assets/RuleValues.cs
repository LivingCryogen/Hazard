using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using System.Collections.ObjectModel;

namespace Model.Assets;

/// <remarks>
/// This implementation is hard-coded and could readily be replaced by '.json' file(s) with converters, a la <see cref="ITroopCard"/>.
/// </remarks>
/// <inheritdoc cref="IRuleValues"/>.
public class RuleValues : IRuleValues
{
    /// <inheritdoc cref="IRuleValues.MinimumArmyBonus"/>.
    public int MinimumArmyBonus { get; } = 3;
    /// <inheritdoc cref="IRuleValues.TerritoryTradeInBonus"/>.
    public int TerritoryTradeInBonus { get; } = 2;
    /// <inheritdoc cref="IRuleValues.AttackersLimit"/>.
    public int AttackersLimit { get; } = 3;
    /// <inheritdoc cref="IRuleValues.DefendersLimit"/>.
    public int DefendersLimit { get; } = 2;
    /// <inheritdoc cref="IRuleValues.ContinentBonus"/>.
    public ReadOnlyDictionary<ContID, int> ContinentBonus { get; } = new(new Dictionary<ContID, int>()
    {
        { ContID.Null, -1 },
        { ContID.NorthAmerica, 5 },
        { ContID.SouthAmerica, 2 },
        { ContID.Europe, 5 },
        { ContID.Africa, 3 },
        { ContID.Asia, 7 },
        { ContID.Oceania, 2 }
    });
    /// <inheritdoc cref="IRuleValues.SetupActionsPerPlayers"/>.
    public ReadOnlyDictionary<int, int> SetupActionsPerPlayers { get; } =
        new(new Dictionary<int, int>()
            { { 2, 78 }, { 3, 105 }, { 4, 120 }, { 5, 125 }, { 6, 120 } });
    /// <inheritdoc cref="IRuleValues.SetupStartingPool"/>.
    public ReadOnlyDictionary<int, int> SetupStartingPool { get; } =
        new(new Dictionary<int, int>()
            { { 2, 26 }, { 3, 35 }, { 4, 30 }, { 5, 25 }, { 6, 20 } });
    /// <inheritdoc cref="IRuleValues.CalculateTerritoryBonus(int)"/>.
    public int CalculateTerritoryBonus(int numTerritories)
    {
        if (numTerritories < 0)
            throw new ArgumentOutOfRangeException(nameof(numTerritories));
        else {
            return numTerritories / 3;
        }
    }
    /// <inheritdoc cref="IRuleValues.CalculateArmyBonus(int, List{ContID})"/>.
    public int CalculateArmyBonus(int numTerritories, List<ContID> continents)
    {
        int bonus = CalculateTerritoryBonus(numTerritories);
        if (bonus < MinimumArmyBonus)
            bonus = MinimumArmyBonus;

        foreach (ContID cont in continents)
            bonus += ContinentBonus[cont];

        return bonus;
    }
    /// <inheritdoc cref="IRuleValues.CalculateBaseTradeInBonus(int)"/>.
    public int CalculateBaseTradeInBonus(int numTrades)
    {
        if (numTrades > 0 && numTrades < 6)
            return 2 + 2 * numTrades; // 1|4, 2|6, 3|8, 4|10, 5|12 -- according to default rules
        else if (numTrades == 6)
            return 15;
        else if (numTrades > 6)
            return 15 + (numTrades - 6) * 5; // 7|20, 8|25, 9|30...
        else return 0;
    }
}
