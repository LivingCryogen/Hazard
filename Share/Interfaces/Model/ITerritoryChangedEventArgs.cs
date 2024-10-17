using Share.Enums;

namespace Share.Interfaces.Model;
/// <summary>
/// Encapsulates data for <see cref="System.EventArgs"/> classes subscribed to <see cref="IBoard.TerritoryChanged"/>.
/// </summary>
public interface ITerritoryChangedEventArgs
{
    /// <summary>
    /// Gets or inits the id of the territory that changed.
    /// </summary>
    /// <value>
    /// A <see cref="TerrID"/>.
    /// </value>
    public TerrID Changed { get; init; }
    /// <summary>
    /// Gets or inits the number of the player now associated with the changed territory.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> if the change involved an see <see cref="IPlayer"/> (ie, if ownership changed); otherwise, <see langword="null"/>.
    /// </value>
    public int? Player { get; init; }
}
