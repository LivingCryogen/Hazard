using Shared.Geography.Enums;

namespace Shared.Interfaces.Model;
/// <summary>
/// Represents a player in the game.
/// </summary>
public interface IPlayer : IBinarySerializable
{
    /// <summary>
    /// Should fire when this IPlayer's property values change.
    /// </summary>
    event EventHandler<IPlayerChangedEventArgs>? PlayerChanged;
    /// <summary>
    /// Fires when this IPlayer has lost the game (by default, when they control no territories).
    /// </summary>
    event EventHandler? PlayerLost;
    /// <summary>
    /// Fires when this IPlayer has won the game.
    /// </summary>
    event EventHandler? PlayerWon;

    #region Properties
    /// <summary>
    /// Gets or sets the name of the player.
    /// </summary>
    string Name { get; set; }
    /// <summary>
    /// Gets the number of the player.
    /// </summary>
    /// <value>
    /// An integer from 0-5. 
    /// </value>
    int Number { get; }
    /// <summary>
    /// Gets a flag indicating whether the player holds a valid trade set of cards.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="Hand"/> contains cards that satisfy their set's <see cref="ICardSet.IsValidTrade(ICard[])"/>; otherwise <see langword="false"/>.</value>
    bool HasCardSet { get; set; }
    /// <summary>
    /// Gets the bonus a player receives to their army pool each turn.
    /// </summary>
    /// <value>
    /// The default rules never allow this to fall below 3 (<see cref="IRuleValues.MinimumArmyBonus"/>.<br/>
    /// It is calculated by <see cref="IRuleValues.CalculateArmyBonus(int, List{ContID})"/>.
    /// </value>
    /// <remarks>
    /// Should be recalculated at the start of each turn (during <see cref="GamePhase.Place"/>)
    /// </remarks>
    int ArmyBonus { get; }
    /// <summary>
    /// Gets or sets the increase to <see cref="ArmyBonus"/> a player receives from controlling continents.
    /// </summary>
    /// <value>
    /// A natural number (0 or positive integer) determined by <see cref="IRuleValues.ContinentBonus"/> and the extent of the player's <see cref="ControlledTerritories"/>.
    /// </value>
    int ContinentBonus { get; set; }
    /// <summary>
    /// Gets or sets the current number of armies a player has left to place.
    /// </summary>
    /// <value>
    /// An integer with an initial value equal to <see cref="ArmyBonus"/> that is reduced when an <see cref="IPlayer"/> places an army during <see cref="GamePhase.Place"/>.
    /// </value>
    int ArmyPool { get; set; }
    /// <summary>
    /// Gets a hashset of territories controlled by the player.
    /// </summary>
    /// <value>
    /// A set of unique <see cref="TerrID"/>s, one for each territory under the player's control.
    /// </value>
    HashSet<TerrID> ControlledTerritories { get; }
    /// <summary>
    /// Gets a list of cards in the player's hand.
    /// </summary>
    /// <value>
    /// Empty by default.
    /// </value>
    List<ICard> Hand { get; }
    #endregion

    /// <summary>
    /// Adds the trade-in bonus to <see cref="ArmyPool"/> when the player trades in cards.
    /// </summary>
    /// <param name="tradeInBonus">The bonus granted.</param>
    void GetsTradeBonus(int tradeInBonus);
    /// <summary>
    /// Gets an array of territories controlled by this player from among a given set.
    /// </summary>
    /// <param name="targets">An array of the territories to match.</param>
    /// <returns>An array of those territories from among <paramref name="targets"/> controlled by this player.</returns>
    TerrID[] GetControlledTargets(TerrID[] targets);
    /// <summary>
    /// Determines whether the player holds a set of <see cref="ICard"/>s that are a tradeable set according to<br/>
    /// <see cref="ICardSet.FindTradeSets(ICard[])"/> and <see cref="ICardSet.IsValidTrade(ICard[])"/>.
    /// </summary>
    void FindCardSet();
    /// <summary>
    /// Adds a territory to this player's control.
    /// </summary>
    /// <param name="territory">The territory to add.</param>
    /// <returns><see langword="true"/> if successfully added; otherwise, <see langword="false"/>.</returns>
    bool AddTerritory(TerrID territory);
    /// <summary>
    /// Removes a territory from this player's control.
    /// </summary>
    /// <param name="territory">A <see cref="TerrID"/> representing the territory to remove.</param>
    /// <returns><see langword="true"/> if successfully removed; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Also fires <see cref="PlayerLost"/> if the count of <see cref="ControlledTerritories"/> falls to 0.
    /// </remarks>
    bool RemoveTerritory(TerrID territory);
    /// <summary>
    /// Adds a card to this player's <see cref="Hand"/>.
    /// </summary>
    /// <param name="card">A <see cref="ICard"/> representing the card to add.</param>
    void AddCard(ICard card);
    /// <summary>
    /// Removes a card from this player's <see cref="Hand"/>.
    /// </summary>
    /// <param name="handIndex">The <see cref="int">index</see> of <see cref="Hand"/> which holds the <see cref="ICard"/> to be removed.</param>
    /// <returns><see langword="true"/> if successfully removed; otherwise, <see langword="false"/>.</returns>
    bool RemoveCard(int handIndex);
}