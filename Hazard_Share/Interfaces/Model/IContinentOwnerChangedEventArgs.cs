namespace Hazard_Share.Interfaces;
using Hazard_Share.Enums;

/// <summary>
/// Recipe for the <see cref="System.EventArgs"/> used by <see cref="Hazard_Model.Entities.EarthBoard.ContinentOwnerChanged"/>.
/// </summary>
public interface IContinentOwnerChangedEventArgs
{
    /// <summary>
    /// The ID of the Continent that changed.
    /// </summary>
    public ContID Changed { get; init; }
    /// <summary>
    /// The <see cref="Hazard_Model.Core.Player.Number"/> of the owner, if any, before the change occurred.
    /// </summary>
    public int? OldPlayer { get; init; }
}
