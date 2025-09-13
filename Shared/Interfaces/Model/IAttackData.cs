using Shared.Geography.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Interfaces.Model;

/// <summary>
/// Represents the data associated with an attack action in the game.
/// </summary>
/// <remarks>This interface provides detailed information about an attack, including the source and target
/// territories, the players involved, the outcome of the attack, and any losses incurred. It also indicates whether the
/// attacker was forced to retreat or successfully conquered the target territory.</remarks>
public interface IAttackData : IActionData
{
    /// <summary>
    /// Gets the source of the attack.
    /// </summary>
    TerrID SourceTerritory { get; }
    /// <summary>
    /// Gets the target of the attack.
    /// </summary>
    TerrID TargetTerritory { get; }
    /// <summary>
    /// Gets the identifier of the player who is currently the attacker.
    /// </summary>
    int Attacker { get => Player; }
    /// <summary>
    /// Gets the player number of the defender.
    /// </summary>
    int Defender { get; }
    /// <summary>
    /// Gets the number of units lost by the attacker.
    /// </summary>
    int AttackerLoss { get; }
    /// <summary>
    /// Gets the number of units lost by the defender.
    /// </summary>
    int DefenderLoss { get; }
    /// <summary>
    /// Gets a flag indicating whether the attacker was forced to retreat (lost all but 1 army).
    /// </summary>
    bool Retreated { get; }
    /// <summary>
    /// Gets a flag indicating whether the attacker conquered the target (defender lost all armies).
    /// </summary>
    bool Conquered { get; }
}
