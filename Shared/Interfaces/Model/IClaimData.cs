using Shared.Geography.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Interfaces.Model;

/// <summary>
/// Represents the data associated with a territory claim, including the player making the claim and the territory being claimed.
/// </summary>
public interface IClaimData : IActionData
{
    /// Gets or inits the identifier of the territory being claimed.
    /// </summary>
    public TerrID TerrClaimed { get; init; }
}
