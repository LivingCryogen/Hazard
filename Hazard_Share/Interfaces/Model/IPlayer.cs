using Hazard_Share.Enums;

namespace Hazard_Share.Interfaces.Model;
/// <summary>
/// A representation of a player in the game.
/// </summary>
public interface IPlayer : IBinarySerializable
{
    /// <summary>
    /// Gets the current bonus the player receives to their army pool at the start of each turn (during <see cref="GamePhase.Place"/>).
    /// </summary>
    /// <value>
    /// An integer. The default rules never allow this to fall below 3 (<see cref="Hazard_Model.Assets.RuleValues.MinimumArmyBonus"/>. <br/>
    /// It is calculated by <see cref="IRuleValues.CalculateArmyBonus(int, List{ContID})"/>.
    /// </value>
    int ArmyBonus { get; }
    /// <summary>
    /// Gets or sets the current number of armies a player has left to place during <see cref="GamePhase.Place"/>.
    /// </summary>
    /// <value>
    /// An integer with an initial value equal to <see cref="ArmyBonus"/> that is reduced when an <see cref="IPlayer"/> places an army during <see cref="GamePhase.Place"/>.
    /// </value>
    int ArmyPool { get; set; }
    /// <summary>
    /// Gets or sets the increase to <see cref="ArmyBonus"/> a player receives from controlling continents.
    /// </summary>
    /// <value>
    /// A natural number (0 or positive integer) determined by <see cref="IRuleValues.ContinentBonus"/>es and the extent of the player's <see cref="ControlledTerritories"/>.
    /// </value>
    int ContinentBonus { get; set; }
    /// <summary>
    /// Gets or sets a list of territories controlled by the player.
    /// </summary>
    /// <value>
    /// A <see cref="List{T}"/> of <see cref="TerrID"/>, one for each territory.
    /// </value>
    List<TerrID> ControlledTerritories { get; set; }
    /// <summary>
    /// Gets or sets a list of cards in the player's hand.
    /// </summary>
    /// <value>
    /// A <see cref="List{T}"/> of <see cref="ICard"/>, one for each card. Default rules initialize this list as empty.
    /// </value>
    List<ICard> Hand { get; set; }
    /// <summary>
    /// Gets or sets the name of the player.
    /// </summary>
    /// <value>
    /// A <see langword="string"/>.
    /// </value>
    string Name { get; set; }
    /// <summary>
    /// Gets the number of the player.
    /// </summary>
    /// <value>
    /// An integer from 0-5. 
    /// </value>
    int Number { get; }
    /// <summary>
    /// Gets a flag that indicates the player is currently holding a set of cards which represent a valid trade.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="IPlayer.Hand"/> contains <see cref="ICard"/>s that satisfy their <see cref="ICardSet"/>'s definitions of a valid trade<br/>
    /// (see <see cref="ICardSet.IsValidTrade(ICard[])"/> and <see cref="ICardSet.FindTradeSets(ICard[])"/>; otherwise <see langword="false"/>.</value>
    bool HasCardSet { get; set; }
    /// <summary>
    /// Should fire when this changes.
    /// </summary>
    event EventHandler<IPlayerChangedEventArgs>? PlayerChanged;
    /// <summary>
    /// Fires when this <see cref="IPlayer"/> has lost the game (by default, when they control no territories).
    /// </summary>
    event EventHandler? PlayerLost;
    /// <summary>
    /// Fires when this <see cref="IPlayer"/> has won the game (by default, when they control a number of territories greater than or equal to the total provided by <see cref="IBoard.Geography"/>).
    /// </summary>
    event EventHandler? PlayerWon;
    /// <summary>
    /// Changes this <see cref="IPlayer"/> when they are given a bonus for card trade-in (ie, increases <see cref="ArmyPool"/>).
    /// </summary>
    /// <param name="tradeInBonus">The bonus granted.</param>
    void GetsTradeBonus(int tradeInBonus);
    /// <summary>
    /// Determines whether the Player currently holds a set of <see cref="ICard"/> that qualifies as a tradeable set according to their <see cref="ICardSet"/> definition. <br/>
    /// (See <see cref="ICardSet.FindTradeSets(ICard[])"/> and <see cref="ICardSet.IsValidTrade(ICard[])"/>.
    /// </summary>
    void FindCardSet();
    /// <summary>
    /// Gets an array of territories controlled by this <see cref="IPlayer"/> from among a given set.
    /// </summary>
    /// <param name="targets">An array of <see cref="TerrID"/> representing the territories to match.</param>
    /// <returns>An array of <see cref="TerrID"/> from among <paramref name="targets"/> controlled by this <see cref="IPlayer"/>.</returns>
    TerrID[] GetControlledTargets(TerrID[] targets);
    /// <summary>
    /// Adds a territory to this player's control.
    /// </summary>
    /// <param name="territory">A <see cref="TerrID"/> representing the territory to add.</param>
    /// <returns><see langword="true"/> if successfully added; otherwise, <see langword="false"/>.</returns>
    bool AddTerritory(TerrID territory);
    /// <summary>
    /// Removes a territory from this player's control.
    /// </summary>
    /// <param name="territory">A <see cref="TerrID"/> representing the territory to remove.</param>
    /// <returns><see langword="true"/> if successfully removed; otherwise, <see langword="false"/>.</returns>
    bool RemoveTerritory(TerrID territory);
    /// <summary>
    /// Adds a card to this player's <see cref="Hand"/>.
    /// </summary>
    /// <param name="card">A <see cref="ICard"/> representing the card to add.</param>
    /// <returns><see langword="true"/> if successfully added; otherwise, <see langword="false"/>.</returns>
    bool AddCard(ICard card);
    /// <summary>
    /// Removes a card from this player's <see cref="Hand"/>.
    /// </summary>
    /// <param name="handIndex">A <see cref="int"/> representing the <see cref="Hand"/> index of the <see cref="ICard"/> to be removed.</param>
    /// <returns><see langword="true"/> if successfully removed; otherwise, <see langword="false"/>.</returns>
    bool RemoveCard(int handIndex);
}