using Shared.Geography.Enums;

namespace Shared.Interfaces;

/// <summary>
/// EventArgs contract as used by <see cref="Model.IBoard.ContinentOwnerChanged"/>.
/// </summary>
public interface IContinentOwnerChangedEventArgs<U> where U : struct, Enum
{
    /// <summary>
    /// Gets or inits a value representing the the Continent that changed.
    /// </summary>
    public U Changed { get; init; }
    /// <summary>
    /// Gets or inits a value representing the Continent's previous owner.
    /// </summary>
    /// <value>
    /// If <see cref="Changed"/> was previously owned by a human player, their <see cref="Model.IPlayer.Number"/>; otherwise, <see langword="null"/>.<br/>
    /// If necessary, no prior ownership may be represented by -1.
    /// </value>    
    public int? OldPlayer { get; init; }
}
