using Share.Enums;
using Share.Interfaces;

namespace Model.EventArgs;

/// <summary>
/// Packages identification data for the <c>ContinentOwnerChanged</c> event and its handlers. See <see cref="Model.Entities.EarthBoard"/>.
/// </summary>
public class ContinentOwnerChangedEventArgs : System.EventArgs, IContinentOwnerChangedEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContinentOwnerChangedEventArgs"/> class when only the Continent ID is required.
    /// </summary>
    /// <param name="changed">The ID of the Continent that has changed.</param>
    public ContinentOwnerChangedEventArgs(ContID changed)
    {
        Changed = changed;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="ContinentOwnerChangedEventArgs"/> class when both the Continent ID and the number of its former owner is required.
    /// </summary>
    /// <param name="changed">The ID of the Continent that has changed.</param>
    /// <param name="oldPlayer">The number of the Player that owned the Continent prior to the change. "None" is represented by -1: see constructor of <see cref="Model.Entities.EarthBoard"/></param>.
    public ContinentOwnerChangedEventArgs(ContID changed, int oldPlayer)
    {
        Changed = changed;
        OldPlayer = oldPlayer;
    }
    /// <value>Property <c>Changed</c> identifies the Continent that has changed.</value>
    public ContID Changed { get; init; }
    /// <value>Property <c>OldPlayer</c> reports the number of the changed Continent's former owner. If said Continent was previously unowned, this property will be null. If necessary, no prior ownership may be represented by -1.</value>
    public int? OldPlayer { get; init; }
}
