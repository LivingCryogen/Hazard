using Shared.Geography.Enums;

namespace Shared.Interfaces.Model;
/// <summary>
/// A contract for EventArgs used by <see cref="IBoard.TerritoryChanged"/>.
/// </summary>
public interface ITerritoryChangedEventArgs 
{
    /// <summary>
    /// Gets or inits the ID of the territory that changed.
    /// </summary>
    public TerrID Changed { get; init; }
    /// <summary>
    /// Gets or inits the number of the player now associated with the changed territory.
    /// </summary>
    /// <value>
    /// <see cref="int">0-5</see> if the change involved an see <see cref="IPlayer"/> (ie, if ownership changed); otherwise, <see langword="null"/>.
    /// </value>
    public int? Player { get; init; }
}
