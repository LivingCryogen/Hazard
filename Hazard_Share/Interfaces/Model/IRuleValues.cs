using Hazard_Share.Enums;
using System.Collections.ObjectModel;

namespace Hazard_Share.Interfaces.Model;

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
    /// Gets the number of bonus armies granted for controlling a given continent (see <see cref="ContID"/>).
    /// </summary>
    /// <value>A <see cref="ReadOnlyDictionary{TKey, TValue}"/> mapping <see cref="ContID"/> keys to <see cref="int"/> values.</value>
    ReadOnlyDictionary<ContID, int> ContinentBonus { get; }
    /// <summary>
    /// Gets the number of actions allowed a player in the Setup phase given the total number of players in the game.
    /// </summary>
    /// <value>A <see cref="ReadOnlyDictionary{TKey, TValue}"/> mapping <see cref="int">number</see> of players to <see cref="int"/> setup actions.</value>
    ReadOnlyDictionary<int, int> SetupActionsPerPlayers { get; }
    /// <summary>
    /// Gets the number of armies a player distributed in the Setup phase given the total number of players in the game.
    /// </summary>
    /// <value>A <see cref="ReadOnlyDictionary{TKey, TValue}"/> mapping <see cref="int">number</see> of players to <see cref="int"/> armies.</value>
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
    /// <param name="numTrades">The number of trades performed so far in this <see cref="IGame"/>.</param>
    /// <returns>The number of additional armies granted.</returns>
    abstract int CalculateBaseTradeInBonus(int numTrades);
    /// <summary>
    /// Calculates how many armies to grant a player at the beginning of their turn.
    /// </summary>
    /// <param name="numTerritories">The number of territories controlled by a <see cref="IPlayer"/>.</param>
    /// <param name="continents">A list of continents (<see cref="ContID"/>) controlled by a <see cref="IPlayer"/>.</param>
    /// <returns>The number of armies granted.</returns>
    public int CalculateArmyBonus(int numTerritories, List<ContID> continents)
    {
        return MinimumArmyBonus + CalculateTerritoryBonus(numTerritories);
    }
}
