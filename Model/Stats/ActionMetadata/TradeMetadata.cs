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
    public List<TerrID> CardTargets { get; set; } = [];
    public int TradeValue { get; set; } = 0;
    public int OccupiedBonus { get; set; } = 0;
    public int Player { get; set; } = -2; // -1 represents AI player, -2 represents uninitialized
}
