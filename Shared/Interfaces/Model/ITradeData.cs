using Shared.Geography.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Interfaces.Model;

/// <summary>
/// Represents the data associated with a trade-in action, including card targets and army rewards.
/// </summary>
/// <remarks>This interface provides information about the trade-in process, including the territories targeted by
/// the traded cards, the base value of the trade, and any additional bonus armies gained from controlling the targeted
/// territories.</remarks>
public interface ITradeData : IActionData
{
    /// <summary>
    /// Gets the targets of the traded cards.
    /// </summary>
    List<TerrID> CardTargets { get; }
    /// <summary>
    /// Gets the base number of armies gained from the trade.
    /// </summary>
    int TradeValue { get; }
    /// <summary>
    /// Gets the bonus armies gained from controlling card targets.
    /// </summary>
    int OccupiedBonus { get; }
}
