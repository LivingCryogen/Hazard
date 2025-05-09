﻿using Shared.Geography.Enums;
using Shared.Interfaces.Model;

namespace Model.EventArgs;
/// <inheritdoc cref="ITerritoryChangedEventArgs"/>
public class TerritoryChangedEventArgs : System.EventArgs, ITerritoryChangedEventArgs
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
    /// <param name="playerNumber">The <see cref="IPlayer.Number">player number</see> of the new owner of the territory.</param>
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
