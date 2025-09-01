using Shared.Geography.Enums;

namespace Shared.Interfaces.Model;

/// <summary>
/// Tracks game session and player statistics via calls parallel to player actions.
/// </summary>
/// <remarks>
/// The calls occur in <see cref="IRegulator"/>.
/// </remarks>
public interface IStatTracker : IBinarySerializable
{
    /// <summary>
    /// Gets the number of actions tracked.
    /// </summary>
    /// <remarks>
    /// Allows <see cref="Model.IStatRepo"/> to easily determine which is the most up-to-date game file for a given game ID.
    /// </remarks>
    public int TrackedActions { get; }
    /// <summary>
    /// Gets the Game Id of the tracker's current game session.
    /// </summary>
    public Guid GameID { get; }
    /// <summary>
    /// Gets the path of the save file last used with this Game.
    /// </summary>
    public string? LastSavePath { get; }

    /// <summary>
    /// Record relevant statistics for an attack.
    /// </summary>
    /// <param name="source">The source of the attack.</param>
    /// <param name="target">The target of the attack.</param>
    /// <param name="conqueredcont">If any, the continent conquered upon attack completion.</param>
    /// <param name="attacker">Player number of the attacker.</param>
    /// <param name="defender">Player number of the defender.</param>
    /// <param name="attackerloss">Armies lost by attacker.</param>
    /// <param name="defenderloss">Armies lost by defender.</param>
    /// <param name="retreated"><see langword="true"/> if the attacker was forced to retreat (lost all but 1 army).</param>
    /// <param name="conquered"><see langword="true"/> if the attacker conquered the target (defender lost all armies).</param>
    public void RecordAttackAction(TerrID source,
        TerrID target,
        ContID? conqueredcont,
        int attacker,
        int defender,
        int attackerloss,
        int defenderloss,
        bool retreated,
        bool conquered);

    /// <summary>
    /// Record relevant statistics for a move.
    /// </summary>
    /// <param name="source">Source of the move.</param>
    /// <param name="target">Target of the move.</param>
    /// <param name="maxAdvanced"><see langword="true"/> if the maximum allowable armies moved.</param>
    /// <param name="player">Number of the player making the move.</param>
    public void RecordMoveAction(TerrID source,
        TerrID target,
        bool maxAdvanced,
        int player);

    /// <summary>
    /// Record relevant statistics for a card trade-in.
    /// </summary>
    /// <param name="cardTargets">Targets of the traded cards.</param>
    /// <param name="tradeValue">Base number of armies gained from the trade.</param>
    /// <param name="occupiedBonus">Bonus armies gained from controlling card targets.</param>
    /// <param name="playerNumber">Number of the player making the trade.</param>
    public void RecordTradeAction(List<TerrID> cardTargets,
        int tradeValue,
        int occupiedBonus,
        int playerNumber);

    /// <summary>
    /// Returns a JSON serialized object version of the underlying statistics data model object.
    /// </summary>
    /// <returns>A JSON string.</returns>
    public Task<string> JSONFromGameSession();
}
