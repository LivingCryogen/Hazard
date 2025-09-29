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
    /// Gets the number of actions currently tracked.
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
    /// Record relevant statistics for an attack.
    /// </summary>
    /// <param name="attackData">Attack metadata to be recorded.</param>
    public void RecordAttackAction(IAttackData attackData);

    /// <summary>
    /// Record relevant statistics for a move.
    /// </summary>
    /// <param name="moveData">Move metadata to be recorded.</param>
    public void RecordMoveAction(IMoveData moveData);

    /// <summary>
    /// Record relevant statistics for a card trade-in.
    /// </summary>
    /// <param name="tradeData">Trade metadata to be recorded.</param>
    public void RecordTradeAction(ITradeData tradeData);

    /// <summary>
    /// Returns a JSON serialized object version of the underlying statistics data model object.
    /// </summary>
    /// <returns>A JSON string.</returns>
    public Task<string> JSONFromGameSession();
}
