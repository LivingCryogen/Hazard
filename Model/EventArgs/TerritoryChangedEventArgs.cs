using Share.Enums;
using Share.Interfaces.Model;

namespace Model.EventArgs;
/// <inheritdoc cref="ITerritoryChangedEventArgs"/>
public class TerritoryChangedEventArgs : System.EventArgs, ITerritoryChangedEventArgs
{
    /// <summary>
    /// Constructs a <see cref="TerritoryChangedEventArgs"/> with only territory ID data.
    /// </summary>
    /// <param name="changed">The <see cref="TerrID"/> of the changed territory.</param>
    public TerritoryChangedEventArgs(TerrID changed)
    {
        Changed = changed;
    }
    /// <summary>
    /// Constructs a <see cref="TerritoryChangedEventArgs"/> with both territory ID and player owner number data.
    /// </summary>
    /// <param name="changed">The <see cref="TerrID"/> of the changed territory.</param>
    /// <param name="playerNumber">The player number of the new owner of the territory as an <see cref="int"/>.</param>
    public TerritoryChangedEventArgs(TerrID changed, int playerNumber)
    {
        Changed = changed;
        Player = playerNumber;
    }
    /// <inheritdoc cref="ITerritoryChangedEventArgs.Changed"/>
    public TerrID Changed { get; init; }
    /// <inheritdoc cref="ITerritoryChangedEventArgs.Player"/>
    public int? Player { get; init; } = null;
}
