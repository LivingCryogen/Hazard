namespace Share.Interfaces;
using Share.Enums;

/// <summary>
/// Recipe for the <see cref="System.EventArgs"/> used by <see cref="Model.Entities.EarthBoard.ContinentOwnerChanged"/>.
/// </summary>
public interface IContinentOwnerChangedEventArgs
{
    /// <summary>
    /// The ID of the Continent that changed.
    /// </summary>
    public ContID Changed { get; init; }
    /// <summary>
    /// The <see cref="Model.Core.Player.Number"/> of the owner, if any, before the change occurred.
    /// </summary>
    public int? OldPlayer { get; init; }
}
