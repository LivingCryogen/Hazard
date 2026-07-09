using Shared.Geography.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats.Metadata;

/// <summary>
/// Represents the data associated with acquiring a continent, including the ID of the action which caused the acquisition, continent ID, previous owner, and new owner.
/// </summary>
public class AcquiredContMetadata
{
    /// <summary>
    /// Gets or inits the ID of the action that caused the continent acquisition.
    /// </summary>
    public int FromActionID { get; init; }
    /// <summary>
    /// Gets or inits the ID of the continent that was acquired.
    /// </summary>
    public ContID Continent { get; init; }
    /// <summary>
    /// Gets or inits the player number of the previous owner of the continent.
    /// </summary>
    public int PrevOwner { get; init; }
    /// <summary>
    /// Gets or inits the player number of the new owner of the continent.
    /// </summary>
    public int NewOwner { get; init; } 
}
