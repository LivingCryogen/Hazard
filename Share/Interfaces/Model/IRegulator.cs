using Share.Enums;

namespace Share.Interfaces.Model;
/// <summary>
/// A service that enforces the rules -- "regulates" on player actions; that is, after the ViewModel interprets player actions based on game state, this enforces <br/>
/// game rule logic in response, updating the state and readying the model for the next input.
/// </summary>
/// <remarks>
/// Currently, as 'facade' presented to the input side of the ViewModel, this contains a variety of bespoke methods. In the future, a Stack of Player Actions <br/>
/// should be implemented, where these Actions are represented by a class with a property that would allow for a Strategy Pattern here. This would then enable <br/>
/// Undo/Redo and Player Action history. The resulting refactor would also hide/remove these methods from this public interface. </remarks>
public interface IRegulator : IBinarySerializable
{
    /// <summary>
    /// Gets or Sets the current limit on player actions during this <see cref="GamePhase"/>.
    /// </summary>
    /// <value>A non-negative integer.</value> 
    int CurrentActionsLimit { get; set; }
    /// <summary>
    /// Gets the number of player actions taken during this <see cref="GamePhase"/>.
    /// </summary>
    /// <value>A non-negative integer.</value> 
    int PhaseActions { get; }
    /// <summary>
    /// Stores the card rewarded to a player for a successful attack.
    /// </summary>
    /// <value>The <see cref="ICard"/> drawn from the library in <see cref="IGame.Cards"/> which will be given to current player at end of turn.<br/> 
    /// If none was earned, <see langword="null"/>.</value>
    ICard? Reward { get; set; }
    /// <summary>
    /// Fires when an <see cref="IPlayer"/> must choose between two or more territories in <see cref="IPlayer.ControlledTerritories"/> to receive a bonus upon card trade-in.
    /// </summary>
    event EventHandler<TerrID[]>? PromptBonusChoice;
    /// <summary>
    /// Fires when an <see cref="IPlayer"/> should be prompted to trade in cards at the start of their turn; may or may not be forced.
    /// </summary>
    event EventHandler<IPromptTradeEventArgs>? PromptTradeIn;
    /// <summary>
    /// Updates game state in response to a territory being selected during a setup <see cref="GamePhase"/> or <see cref="GamePhase.Place"/>.
    /// </summary>
    /// <param name="territory">The <see cref="TerrID"/> of the territory selected.</param>
    void ClaimOrReinforce(TerrID territory);
    /// <summary>
    /// Updates the game state in response to the 'move' action, in which a player moves a select number of armies from one controlled territory to another during <see cref="GamePhase.Move"/>.
    /// </summary>
    /// <param name="source">The <see cref="TerrID"/> identifying the source of the moving armies.</param>
    /// <param name="target">The <see cref="TerrID"/> identifying the target of the move.</param>
    /// <param name="armies">An <see cref="int"/> representing the number of armies to move.</param>
    void MoveArmies(TerrID source, TerrID target, int armies);
    /// <summary>
    /// Determines whether a subset of a player's hand can be traded in.
    /// </summary>
    /// <param name="player">An <see cref="int"/> representing the number of the player whose hand contains the cards to be traded.</param>
    /// <param name="handIndices">An array of <see cref="int"/>s representing the indices of the <see cref="ICard"/>s in the <see cref="IPlayer.Hand"/> collection to be traded.</param>
    /// <returns><see langword="true"/> if circumstances allow the trade; otherwise, <see langword="false"/>. <br/>
    /// Typically, relevant circumstances include the <see cref="ICard"/> values, their <see cref="ICardSet"/> trading functions, and the current <see cref="GamePhase"/>.</returns>
    bool CanTradeInCards(int player, int[] handIndices);
    /// <summary>
    /// Executes game-rule logic for trading in cards from a player.
    /// </summary>
    /// <param name="player">The number of the <see cref="IPlayer"/> trading their cards.</param>
    /// <param name="handIndices">An array representing the indices of <see cref="IPlayer.Hand"/> which were traded.</param>
    void TradeInCards(int player, int[] handIndices);
    /// <summary>
    /// Executes game-rule logic when given the results of a battle between the armies of two territories.
    /// </summary>
    /// <param name="source">The <see cref="TerrID"/> representing the source of the attack (attacker).</param>
    /// <param name="target">The <see cref="TerrID"/> representing the target of the attack (defender).</param>
    /// <param name="attackResults">An array containing the results of the attacker's dice rolls.</param>
    /// <param name="defenseResults">An array containing the results of the defender's dice rolls.</param>
    /// <returns>A <see cref="ValueTuple{T1, T2}"/>, where T1 is the number of armies lost by the attacker (source of the battle), and T2 is that of the defender (target).</returns>
    void Battle(TerrID source, TerrID target, int[] attackResults, int[] defenseResults);
    /// <summary>
    /// Executes game-rule logic for awarding bonus armies to a territory due to card trade-in (e.g. when the player controls one or more territories of a <see cref="ITroopCard"/> upon trade-in.
    /// </summary>
    /// <param name="territory">The <see cref="TerrID"/> of the territory awarded the bonus armies.</param>
    void AwardTradeInBonus(TerrID territory);
    /// <summary>
    /// Delivers the <see cref="ICard"/> held in <see cref="Reward"/>, if any, to the appropriate <see cref="IPlayer"/>.
    /// </summary>
    void DeliverCardReward();
    /// <summary>
    /// Initializes this instance with a brand new game.
    /// </summary>
    /// <param name="game">The newly created <see cref="IGame"/>.</param>
    abstract void Initialize();
}
