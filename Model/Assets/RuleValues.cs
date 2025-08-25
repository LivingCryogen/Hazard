using Microsoft.Extensions.Options;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Configuration;
using System.Collections.ObjectModel;

namespace Model.Assets;

/// <inheritdoc cref="IRuleValues"/>.
public class RuleValues(IOptions<AppConfig> options) : IRuleValues
{
    /// <inheritdoc cref="IRuleValues.StartingArmies"/>.
    public int StartingArmies { get; init; } = options.Value.RuleValues?.StartingArmies ?? 0;
    /// <inheritdoc cref="IRuleValues.MinimumArmyBonus"/>.
    public int MinimumArmyBonus { get; } = options.Value.RuleValues?.MinimumArmyBonus ?? 0;
    /// <inheritdoc cref="IRuleValues.TerritoryTradeInBonus"/>.
    public int TerritoryTradeInBonus { get; } = options.Value.RuleValues?.TerritoryTradeInBonus ?? 0;
    /// <inheritdoc cref="IRuleValues.AttackersLimit"/>.
    public int AttackersLimit { get; } = options.Value.RuleValues?.AttackersLimit ?? 0;
    /// <inheritdoc cref="IRuleValues.DefendersLimit"/>.
    public int DefendersLimit { get; } = options.Value.RuleValues?.DefendersLimit ?? 0;
    /// <inheritdoc cref="IRuleValues.ContinentBonus"/>.
    public ReadOnlyDictionary<ContID, int> ContinentBonus { get; } =
        new(new Dictionary<ContID, int>(
            options.Value.RuleValues?.ContinentBonuses.Select((keypairs) =>
                KeyValuePair.Create(Enum.Parse<ContID>(keypairs.Key, ignoreCase: true), keypairs.Value)) ?? []));

    /// <inheritdoc cref="IRuleValues.SetupActionsPerPlayers"/>.
    public ReadOnlyDictionary<int, int> SetupActionsPerPlayers { get; } =
        new(new Dictionary<int, int>(
            options.Value.RuleValues?.SetupActionsPerPlayers.Select((keypairs) =>
                KeyValuePair.Create(int.Parse(keypairs.Key), keypairs.Value)) ?? []));

    /// <inheritdoc cref="IRuleValues.SetupStartingPool"/>.
    public ReadOnlyDictionary<int, int> SetupStartingPool { get; } =
        new(new Dictionary<int, int>(
            options.Value.RuleValues?.SetupStartingPool.Select((keypairs) =>
                KeyValuePair.Create(int.Parse(keypairs.Key), keypairs.Value)) ?? []));

    /// <inheritdoc cref="IRuleValues.CalculateTerritoryBonus(int)"/>.
    public int CalculateTerritoryBonus(int numTerritories)
    {
        if (numTerritories < 0)
            throw new ArgumentOutOfRangeException(nameof(numTerritories));
        else
        {
            return numTerritories / 3;
        }
    }
    /// <inheritdoc cref="IRuleValues.CalculateArmyBonus"/>.
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
    public int CalculateBaseTradeInBonus(int numTrades) => numTrades switch
    {
        < 0 => 0,
        < 6 => 2 + 2 * numTrades, // 1|4, 2|6, 3|8, 4|10, 5|12 -- according to default rules
        6 => 15,
        > 6 => 15 + (numTrades - 6) * 5,// 7|20, 8|25, 9|30...
    };
}
