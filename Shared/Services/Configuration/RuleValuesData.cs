namespace Shared.Services.Configuration;

/// <summary>
/// Destination for static configuration data of Rule Values, to be consumed by <see cref="Interfaces.Model.IRuleValues"/> implementations.
/// </summary>
public class RuleValuesData
{
    /// <summary>
    /// The number of armies each territory starts with.
    /// </summary>
    public int StartingArmies { get; set; } = 0;
    /// <summary>
    /// The minimum number of armies added to a player's placement count at the beginning of a turn.
    /// </summary>
    public int MinimumArmyBonus { get; set; } = 0;
    /// <summary>
    /// The number of armies gained when a player trades in a set of cards.
    /// </summary>
    public int TerritoryTradeInBonus { get; set; } = 0;
    /// <summary>
    /// The maximum number of dice allowed for rolling attacks.
    /// </summary>
    public int AttackersLimit { get; set; } = 0;
    /// <summary>
    /// The maximum number of dice allowed for rolling on defense.
    /// </summary>
    public int DefendersLimit { get; set; } = 0;
    /// <summary>
    /// The number of armies a player gains each turn that they control a given Continent.
    /// </summary>
    public Dictionary<string, int> ContinentBonuses { get; set; } = [];
    /// <summary>
    /// The number of actions each player has during Setup given the number of players in the game.
    /// </summary>
    public Dictionary<string, int> SetupActionsPerPlayers { get; set; } = [];
    /// <summary>
    /// The number of armies in each player's starting pool given the number of players in the game.
    /// </summary>
    public Dictionary<string, int> SetupStartingPool { get; set; } = [];
}
