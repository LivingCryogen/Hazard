using Shared.Geography.Enums;
using System.Collections.ObjectModel;

namespace Shared.Interfaces.Model;

/// <summary>
/// Provides constants and equations derived from game rules.
/// </summary>
public interface IRuleValues
{
    /// <summary>
    /// Gets the minimum number to add to a player's army count on their turn.
    /// </summary>
    /// <value>A positive integer.</value>
    int MinimumArmyBonus { get; }
    /// <summary>
    /// Gets the number of armies to add to a player's army count if they trade in a set of cards.
    /// </summary>
    /// <value>A positive integer.</value>
    int TerritoryTradeInBonus { get; }
    /// <summary>
    /// Gets the maximum number of dice allowed an attacking player.
    /// </summary>
    /// <value>A positive integer.</value>
    int AttackersLimit { get; }
    /// <summary>
    /// Gets the maximum number of dice allowed a defending player.
    /// </summary>
    /// <value>A positive integer.</value>
    int DefendersLimit { get; }
    /// <summary>
    /// Gets map of the number of bonus armies granted for controlling a given continent (see <see cref="ContID"/>).
    /// </summary>
    ReadOnlyDictionary<ContID, int> ContinentBonus { get; }
    /// <summary>
    /// Gets a map of the number of actions allowed a player in the Setup phase given the total number of players.
    /// </summary>
    ReadOnlyDictionary<int, int> SetupActionsPerPlayers { get; }
    /// <summary>
    /// Gets the number of armies distributed to a player during setup given the total number of players.
    /// </summary>
    ReadOnlyDictionary<int, int> SetupStartingPool { get; }
    /// <summary>
    /// Calculates how many additional armies to grant a player based upon the number of territories they control.
    /// </summary>
    /// <param name="numTerritories">The number of territories under control of a player.</param>
    /// <returns>The total number of armies granted.</returns>
    abstract int CalculateTerritoryBonus(int numTerritories);
    /// <summary>
    /// Calculates how many additional armies to grant a player when they trade in set of cards on their turn.
    /// </summary>
    /// <param name="numTrades">The number of trades performed so far.</param>
    /// <returns>The number of additional armies granted.</returns>
    abstract int CalculateBaseTradeInBonus(int numTrades);
    /// <summary>
    /// Calculates how many armies to grant a player at the beginning of their turn.
    /// </summary>
    /// <param name="numTerritories">The number of territories controlled by a player.</param>
    /// <param name="continents">A list of continents controlled by a player.</param>
    /// <returns>The number of armies granted.</returns>
    public int CalculateArmyBonus(int numTerritories, List<ContID> continents)
    {
        return MinimumArmyBonus + CalculateTerritoryBonus(numTerritories);
    }
}
