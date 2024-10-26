using Share.Enums;
using Share.Interfaces;

namespace Model.EventArgs;

/// <summary>
/// Packages identification data for <see cref="Entities.EarthBoard.ContinentOwnerChanged"/> and its handlers.
/// </summary>
public class ContinentOwnerChangedEventArgs : System.EventArgs, IContinentOwnerChangedEventArgs
{
    /// <summary>
    /// Constructs a new ContinentOwnerChangedEventArgs when only the Continent ID is required.
    /// </summary>
    /// <param name="changed">The ID of the Continent that has changed.</param>
    public ContinentOwnerChangedEventArgs(ContID changed)
    {
        Changed = changed;
    }
    /// <summary>
    /// Constructs a new ContinentOwnerChangedEventArgs when both the Continent ID and the number of its former owner is required.
    /// </summary>
    /// <param name="changed">The ID of the Continent that has changed.</param>
    /// <param name="oldPlayer">The number of the Player that owned the Continent prior to the change. "None" is represented by -1: see constructor of <see cref="Entities.EarthBoard"/></param>.
    public ContinentOwnerChangedEventArgs(ContID changed, int oldPlayer)
    {
        Changed = changed;
        OldPlayer = oldPlayer;
    }
    /// <inheritdoc cref="IContinentOwnerChangedEventArgs.Changed"/>
    public ContID Changed { get; init; }
    /// <inheritdoc cref="IContinentOwnerChangedEventArgs.OldPlayer"/>
    public int? OldPlayer { get; init; }
}
