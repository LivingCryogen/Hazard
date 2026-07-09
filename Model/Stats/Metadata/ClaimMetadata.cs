using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats.Metadata;

/// <inheritdoc cref="IClaimData"/>
public class ClaimMetadata : IClaimData
{
    /// <summary>
    /// 
    /// </summary>
    public int Player { get; init; } = -2;
    /// <summary>
    /// Gets or inits the identifier of the territory being claimed.
    /// </summary>
    public TerrID TerrClaimed { get; init; }
}
