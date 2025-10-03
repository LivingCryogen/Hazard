using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats.ActionMetadata;

/// <inheritdoc cref="IAttackData"/>
public class AttackMetadata : IAttackData
{
    /// <summary>
    /// Gets or sets the number of the player performing the action.
    /// </summary>
    /// <remarks>-1 represents AI player, -2 represents uninitialized</remarks>
    public int Player { get; set; } = -2;
    /// <summary>
    /// Gets or sets the source of the attack.
    /// </summary>
    public TerrID SourceTerritory { get; set; } = TerrID.Null;
    /// <summary>
    /// Gets or sets the target of the attack.
    /// </summary>
    public TerrID TargetTerritory { get; set; } = TerrID.Null;
    /// <summary>
    /// Gets the player performing the action (the attacker).
    /// </summary>
    public int Attacker => Player;
    /// <summary>
    /// Gets or sets the player number of the defender.
    /// </summary>
    /// <remarks>-1 represents AI player, -2 represents uninitialized</remarks>
    public int Defender { get; set; } = -2;
    /// <summary>
    /// Gets or sets the number of armies the attacker had prior to the attack.
    /// </summary>
    public int AttackerInitialArmies { get; set; } = 0;
    /// <summary>
    /// Gets or sets the number of armies the defender had prior to the attack.
    /// </summary>
    public int DefenderInitialArmies { get; set; } = 0;
    /// <summary>
    /// Gets or sets the number of dice rolled by the attacker.
    /// </summary>
    public int AttackerDice { get; set;} = 0;
    /// <summary>
    /// Gets or sets the number of dice rolled by the defender.
    /// </summary>
    public int DefenderDice { get; set; } = 0;
    /// <summary>
    /// The number of armies lost by the attacker.
    /// </summary>
    public int AttackerLoss { get; set; } = 0;
    /// <summary>
    /// Gets or sets the number of armies lost by the defender.
    /// </summary>
    public int DefenderLoss { get; set; } = 0;
    /// <summary>
    /// Gets or sets a value indicating whether the attacker was forced to retreat (lost all but 1 army).
    /// </summary>
    public bool Retreated { get; set; } = false;
    /// <summary>
    /// Gets or sets a value indicating whether the attacker conquered the target (defender lost all armies).
    /// </summary>
    public bool Conquered { get; set; } = false;
}
