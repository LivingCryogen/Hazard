using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats.Metadata;

/// <inheritdoc cref="IMoveData"/>
public class MoveMetadata : IMoveData
{
    /// <summary>
    /// Gets or sets the identifier for the source of the move.
    /// </summary>
    public TerrID SourceTerritory { get; set; } = TerrID.Null;
    /// <summary>
    /// Gets or sets the identifier for the target of the move.
    /// </summary>
    public TerrID TargetTerritory { get; set; } = TerrID.Null;
    /// <summary>
    /// Gets or sets a value indicating whether the maximum possible number of armies were moved.
    /// </summary>
    public bool MaxAdvanced { get; set; } = false;
    /// <summary>
    /// Gets or sets the number of the player making the move.
    /// </summary>
    public int Player { get; set; } = -2; // -1 represents AI player, -2 represents uninitialized
}
