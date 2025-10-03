using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Interfaces.Model;

/// <summary>
/// Represents data associated with an action performed by a player.
/// </summary>
/// <remarks>This interface provides a way to access the number of the player that performed an action.
/// Extensions of this interface can include additional details specific to the action.</remarks>
public interface IActionData
{
    /// <summary>
    /// Gets the player number of the player performing the action.
    /// </summary>
    int Player { get; }
}
