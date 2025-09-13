using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Interfaces.Model;

public interface IActionData
{
    /// <summary>
    /// Gets the player number of the player performing the action.
    /// </summary>
    int Player { get; }
}
