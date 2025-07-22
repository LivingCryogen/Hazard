using Shared.Geography.Enums;
using Shared.Interfaces.Model;

namespace Model.EventArgs;
/// <inheritdoc cref="ITerritoryChangedEventArgs{T}"/>
public class TerritoryChangedEventArgs : System.EventArgs, ITerritoryChangedEventArgs<TerrID>
{
    /// <summary>
    /// Constructs a TerritoryChangedEventArgs with only territory ID data.
    /// </summary>
    /// <param name="changed">The ID of the changed territory.</param>
    public TerritoryChangedEventArgs(TerrID changed)
    {
        Changed = changed;
    }
    /// <summary>
    /// Constructs a TerritoryChangedEventArgs with both territory ID and player owner number data.
    /// </summary>
    /// <param name="changed">The ID of the changed territory.</param>
    /// <param name="playerNumber">The <see cref="IPlayer{T}.Number">player number</see> of the new owner of the territory.</param>
    public TerritoryChangedEventArgs(TerrID changed, int playerNumber)
    {
        Changed = changed;
        Player = playerNumber;
    }
    /// <inheritdoc cref="ITerritoryChangedEventArgs{T}.Changed"/>
    public TerrID Changed { get; init; }
    /// <inheritdoc cref="ITerritoryChangedEventArgs{T}.Player"/>
    public int? Player { get; init; } = null;
}
