using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats.ActionMetadata;
/// <summary>
/// Represents metadata for a move, including source and target territories, player information, and advanced move
/// status.
/// </summary>
/// <remarks>This class provides details about a move in the game, such as the originating and destination
/// territories,  the player making the move, and whether the move is marked as maximally advanced. </remarks>
public class MoveMetadata : IMoveData
{
    public TerrID SourceTerritory { get; set; } = TerrID.Null;
    public TerrID TargetTerritory { get; set; } = TerrID.Null;
    public bool MaxAdvanced { get; set; } = false;
    public int Player { get; set; } = -2; // -1 represents AI player, -2 represents uninitialized
}
