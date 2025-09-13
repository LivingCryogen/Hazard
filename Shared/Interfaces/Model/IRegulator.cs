using Shared.Geography.Enums;

namespace Shared.Interfaces.Model;
/// <summary>
/// Facade for the Model's user interactions.
/// </summary>
/// <remarks>
/// Enforces the game rules -- "regulates" player actions; that is, after the ViewModel interprets player actions based on game state, this enforces <br/>
/// game rule logic in response, updating the state and readying the model for the next input.
/// </remarks>
public interface IRegulator : IBinarySerializable
{
    /// <summary>
    /// Gets or sets the limit on player actions during this <see cref="Enums.GamePhase"/>.
    /// </summary>
    int CurrentActionsLimit { get; set; }
    /// <summary>
    /// Gets the number of player actions taken during this <see cref="Enums.GamePhase"/>.
    /// </summary>
    int PhaseActions { get; }
    /// <summary>
    /// Gets or sets a flag indicating whether an ICard reward is waiting to be delivered at end of turn.
    /// </summary>
    /// <remarks>
    /// See <see cref="ICardBase.Reward"/>.
    /// </remarks>
    /// <value>If the current player made a successful attack, an <see cref="ICard"/>. Otherwise, <see langword="null"/>.</value>
    bool RewardPending { get; set; }
    /// <summary>
    /// Fires when a player must choose between two or more territories they control to receive a bonus upon card trade-in.
    /// </summary>
    event EventHandler<TerrID[]>? PromptBonusChoice;
    /// <summary>
    /// Fires when a player should be prompted to trade in cards at the start of their turn; may or may not be forced.
    /// </summary>
    event EventHandler<IPromptTradeEventArgs>? PromptTradeIn;
    /// <summary>
    /// Determines whether a given territory is a valid selection according to rules and game state.
    /// </summary>
    /// <param name="newSelected">The territory being selected.</param>
    /// <param name="oldSelected">The territory already selected, if any.</param>
    /// <returns><see langword="true"/> if the territory is a valid selection; otherwise, <see langword="false"/>.</returns>
    bool CanSelectTerritory(TerrID newSelected, TerrID oldSelected);
    /// <summary>
    /// Receives a territory ID selection as Player input and determines course of game logic.
    /// </summary>
    /// <param name="selected">The Territory selected by the player.</param>
    /// <param name="priorSelected">The previous selection.</param>
    /// <returns>A tuple containing the updated selection and a flag for requesting further player input (needed in Attack and Move phases). 
    /// If MaxValue is not null, the request is for Move/Advance.
    /// </returns>
    public (TerrID Selection, bool RequestInput, int? MaxValue) SelectTerritory(TerrID selected, TerrID priorSelected);
    /// <summary>
    /// Updates game state in response to a territory being selected during Setup or Place phases (see <see cref="Enums.GamePhase"/>).
    /// </summary>
    /// <param name="territory">The territory selected.</param>
    void ClaimOrReinforce(TerrID territory);
    /// <summary>
    /// Updates the game state in response to the 'Move' action, in which a player moves a number of armies from one controlled territory to another during <see cref="Enums.GamePhase.Move"/>.
    /// </summary>
    /// <param name="source">The source of the moving armies.</param>
    /// <param name="target">The target of the move.</param>
    /// <param name="armies">The number of armies to move.</param>
    void MoveArmies(TerrID source, TerrID target, int armies);
    /// <summary>
    /// Determines whether a subset of a player's hand can be traded in.
    /// </summary>
    /// <param name="player">The number of the player whose hand contains the cards to be traded.</param>
    /// <param name="handIndices">The indices of the <see cref="ICard"/>s within <see cref="IPlayer.Hand"/> that are to be traded.</param>
    /// <returns><see langword="true"/> if circumstances allow the trade; otherwise, <see langword="false"/>. <br/></returns>
    /// <remarks>Typically, relevant factors include the <see cref="ICard"/> values, their <see cref="ICardSet"/> trading functions, and the current <see cref="Enums.GamePhase"/>.</remarks>
    bool CanTradeInCards(int player, int[] handIndices);
    /// <summary>
    /// Executes game-rule logic for trading in cards from a player.
    /// </summary>
    /// <param name="player">The <see cref="int">number</see> of the player trading their cards.</param>
    /// <param name="handIndices">An array representing the indices of <see cref="IPlayer.Hand"/> which were traded.</param>
    void TradeInCards(int player, int[] handIndices);
    /// <summary>
    /// Executes game-rule logic when given the results of a battle between the armies of two territories.
    /// </summary>
    /// <param name="source">The source of the attack (attacker).</param>
    /// <param name="target">The target of the attack (defender).</param>
    /// <param name="diceRolls">Dice rolls paired together according to game rules.</param>
    /// <param name="numAttackDice">Number of attack dice rolled.</param>
    /// <remarks>
    /// By default, rolls for attacker and defender should be put in descending order, then paired (unpaired remainders are ignored).
    /// </remarks>
    void Battle(TerrID source, TerrID target, (int AttackRoll, int DefenseRoll)[] diceRolls, int numAttackDice);
    /// <summary>
    /// Executes game-rule logic for awarding bonus armies to a territory due to card trade-in.
    /// </summary>
    /// <remarks>
    /// Used when a player controls one or more territories of the <see cref="ICard"/>s upon trade-in.
    /// </remarks>
    /// <param name="territory">The territory awarded the bonus armies.</param>
    void AwardTradeInBonus(TerrID territory);
    /// <summary>
    /// Delivers the <see cref="ICard"/> held in <see cref="ICardBase.Reward"/>, if any, to the appropriate <see cref="IPlayer"/>.
    /// </summary>
    void DeliverCardReward();
    /// <summary>
    /// Initializes this <see cref="IRegulator"/>.
    /// </summary>
    /// <remarks>
    /// This post-construction initialization step is needed to accomodate loading from save files.
    /// </remarks>
    abstract void Initialize();
}
