using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats.ActionMetadata;

/// <summary>
/// 
/// </summary>
public class TradeMetadata : ITradeData
{
    /// <summary>
    /// Gets or sets the list of card targets involved in the trade.
    /// </summary>
    public List<TerrID> CardTargets { get; set; } = [];
    /// <summary>
    /// Gets or sets the number of armies received.
    /// </summary>
    public int TradeValue { get; set; } = 0;
    /// <summary>
    /// Gets or sets the bonus armies received for controlling target territories.
    /// </summary>
    public int OccupiedBonus { get; set; } = 0;
    /// <summary>
    /// Gets or sets the number of the player making the trade.
    /// </summary>
    public int Player { get; set; } = -2; // -1 represents AI player, -2 represents uninitialized
}
