﻿using Shared.Geography.Enums;

namespace Shared.Interfaces.Model;
/// <summary>
/// Enforces the game rules -- "regulates" player actions; that is, after the ViewModel interprets player actions based on game state, this enforces <br/>
/// game rule logic in response, updating the state and readying the model for the next input.
/// </summary>
public interface IRegulator : IBinarySerializable
{
    /// <summary>
    /// Gets the limit on player actions during this <see cref="GamePhase"/>.
    /// </summary>
    int CurrentActionsLimit { get; set; }
    /// <summary>
    /// Gets the number of player actions taken during this <see cref="GamePhase"/>.
    /// </summary>
    int PhaseActions { get; }
    /// <summary>
    /// Stores the card rewarded to a player for a successful attack.
    /// </summary>
    /// <value>If the current player made a successful attack, an <see cref="ICard"/>. Otherwise, <see langword="null"/>.</value>
    ICard? Reward { get; set; }
    /// <summary>
    /// Fires when a player must choose between two or more territories in <see cref="IPlayer.ControlledTerritories"/> to receive a bonus upon card trade-in.
    /// </summary>
    event EventHandler<TerrID[]>? PromptBonusChoice;
    /// <summary>
    /// Fires when a player should be prompted to trade in cards at the start of their turn; may or may not be forced.
    /// </summary>
    event EventHandler<IPromptTradeEventArgs>? PromptTradeIn;
    /// <summary>
    /// Updates game state in response to a territory being selected during Setup or Place phases (see <see cref="GamePhase"/>).
    /// </summary>
    /// <param name="territory">The <see cref="TerrID"/> of the territory selected.</param>
    void ClaimOrReinforce(TerrID territory);
    /// <summary>
    /// Updates the game state in response to the 'Move' action, in which a player moves a number of armies from one controlled territory to another during <see cref="GamePhase.Move"/>.
    /// </summary>
    /// <param name="source">The source of the moving armies.</param>
    /// <param name="target">The target of the move.</param>
    /// <param name="armies">The <see cref="int">number</see> of armies to move.</param>
    void MoveArmies(TerrID source, TerrID target, int armies);
    /// <summary>
    /// Determines whether a subset of a player's hand can be traded in.
    /// </summary>
    /// <param name="player">The <see cref="int">number</see> of the player whose hand contains the cards to be traded.</param>
    /// <param name="handIndices">An array of <see cref="int">indices</see> of the <see cref="ICard"/>s within <see cref="IPlayer.Hand"/> that are to be traded.</param>
    /// <returns><see langword="true"/> if circumstances allow the trade; otherwise, <see langword="false"/>. <br/></returns>
    /// <remarks>Typically, relevant factors include the <see cref="ICard"/> values, their <see cref="ICardSet"/> trading functions, and the current <see cref="GamePhase"/>.</remarks>
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
    /// <param name="diceRolls">An array of <see cref="ValueTuple{T1, T2}">dice rolls</see> that have been matched according to game rules.</param>
    /// <remarks>
    /// By default, rolls for attacker and defender should be put in descending order, then paired (remainders are ignored).
    /// </remarks>
    void Battle(TerrID source, TerrID target, (int AttackRoll, int DefenseRoll)[] diceRolls);
    /// <summary>
    /// Executes game-rule logic for awarding bonus armies to a territory due to card trade-in.
    /// </summary>
    /// <remarks>
    /// Used when a player controls one or more territories of the <see cref="ICard.Target"/>s upon trade-in.
    /// </remarks>
    /// <param name="territory">The territory awarded the bonus armies.</param>
    void AwardTradeInBonus(TerrID territory);
    /// <summary>
    /// Delivers the <see cref="ICard"/> held in <see cref="Reward"/>, if any, to the appropriate <see cref="IPlayer"/>.
    /// </summary>
    void DeliverCardReward();
    /// <summary>
    /// Initializes an <see cref="IRegulator"/>.
    /// </summary>
    /// <remarks>
    /// This post-construction initialization step is needed to accomodate loading from save files.
    /// </remarks>
    abstract void Initialize();
}