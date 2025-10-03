using Shared.Geography.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Interfaces.Model;

/// <summary>
/// Represents move action data, including the source and target territories and whether the maximum
/// allowable armies were moved.
/// </summary>
public interface IMoveData : IActionData
{
    /// <summary>
    /// Gets the source of the move.
    /// </summary>
    TerrID SourceTerritory { get; }
    /// <summary>
    /// Gets the target of the move.
    /// </summary>
    TerrID TargetTerritory { get; }
    /// <summary>
    /// Gets a flag indicating whether the maximum allowable armies were moved.
    /// </summary>
    bool MaxAdvanced { get; }
}
