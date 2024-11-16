using Microsoft.Extensions.Configuration;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using System.Collections.ObjectModel;

namespace Model.Assets;

/// <remarks>
/// This implementation is hard-coded and could readily be replaced by '.json' file(s) with converters, a la <see cref="ITroopCard"/>.
/// </remarks>
/// <inheritdoc cref="IRuleValues"/>.
public class RuleValues(IConfiguration config) : IRuleValues
{
    /// <inheritdoc cref="IRuleValues.MinimumArmyBonus"/>.
    public int MinimumArmyBonus { get; } = int.Parse(config["MinimumArmyBonus"] ?? "");
    /// <inheritdoc cref="IRuleValues.TerritoryTradeInBonus"/>.
    public int TerritoryTradeInBonus { get; } = int.Parse(config["TerritoryTradeInBonus"] ?? "");
    /// <inheritdoc cref="IRuleValues.AttackersLimit"/>.
    public int AttackersLimit { get; } = int.Parse(config["AttackersLimit"] ?? "");
    /// <inheritdoc cref="IRuleValues.DefendersLimit"/>.
    public int DefendersLimit { get; } = int.Parse(config["DefendersLimit"] ?? "");
    /// <inheritdoc cref="IRuleValues.ContinentBonus"/>.
    public ReadOnlyDictionary<ContID, int> ContinentBonus { get; } =
        new(new Dictionary<ContID, int>() {
                { ContID.Null, -1 },
                { ContID.NorthAmerica, int.Parse(config.GetSection("ContinentBonuses")["NorthAmericaBonus"] ?? "") },
                { ContID.SouthAmerica, int.Parse(config.GetSection("ContinentBonuses")["SouthAmericaBonus"] ?? "") },
                { ContID.Europe, int.Parse(config.GetSection("ContinentBonuses")["EuropeBonus"] ?? "") },
                { ContID.Africa, int.Parse(config.GetSection("ContinentBonuses")["AfricaBonus"] ?? "") },
                { ContID.Asia, int.Parse(config.GetSection("ContinentBonuses")["AsiaBonus"] ?? "") },
                { ContID.Oceania, int.Parse(config.GetSection("ContinentBonuses")["OceaniaBonus"] ?? "") }
            }
        );
    /// <inheritdoc cref="IRuleValues.SetupActionsPerPlayers"/>.
    public ReadOnlyDictionary<int, int> SetupActionsPerPlayers { get; } =
        new(new Dictionary<int, int>() {
                { 2, int.Parse(config.GetSection("SetupActionsPerPlayers")["Two"] ?? "") },
                { 3, int.Parse(config.GetSection("SetupActionsPerPlayers")["Three"] ?? "") },
                { 4, int.Parse(config.GetSection("SetupActionsPerPlayers")["Four"] ?? "") },
                { 5, int.Parse(config.GetSection("SetupActionsPerPlayers")["Five"] ?? "") },
                { 6, int.Parse(config.GetSection("SetupActionsPerPlayers")["Six"] ?? "") }
            }
        );
    /// <inheritdoc cref="IRuleValues.SetupStartingPool"/>.
    public ReadOnlyDictionary<int, int> SetupStartingPool { get; } =
        new(new Dictionary<int, int>() {
                { 2, int.Parse(config.GetSection("SetupStartingPool")["Two"] ?? "") },
                { 3, int.Parse(config.GetSection("SetupStartingPool")["Three"] ?? "") },
                { 4, int.Parse(config.GetSection("SetupStartingPool")["Four"] ?? "") },
                { 5, int.Parse(config.GetSection("SetupStartingPool")["Five"] ?? "") },
                { 6, int.Parse(config.GetSection("SetupStartingPool")["Six"] ?? "") }
            }
        );
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
