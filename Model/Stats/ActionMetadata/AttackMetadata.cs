using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats.ActionMetadata;

public class AttackMetadata : IAttackData
{
    public int Player { get; set; } = -2; // -1 represents AI player, -2 represents uninitialized
    public TerrID SourceTerritory { get; set; } = TerrID.Null;
    public TerrID TargetTerritory { get; set; } = TerrID.Null;
    public int Defender { get; set; } = -2; // -1 represents AI player, -2 represents uninitialized
    public int AttackerInitialArmies { get; set; } = 0;
    public int DefenderInitialArmies { get; set; } = 0;
    public int AttackerDice { get; set;} = 0;
    public int DefenderDice { get; set; } = 0;
    public int AttackerLoss { get; set; } = 0;
    public int DefenderLoss { get; set; } = 0;
    public bool Retreated { get; set; } = false;
    public bool Conquered { get; set; } = false;
}
