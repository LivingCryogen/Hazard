using Shared.Geography.Enums;
using System.Collections.ObjectModel;

namespace Shared.Interfaces.ViewModel;
/// <summary>
/// Defines public data for ViewModels that present <see cref="Model.IPlayer"/> data.
/// </summary>
public interface IPlayerData
{
    /// <summary>
    /// Gets or sets the player's name.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the name of the player's color.
    /// </summary>
    /// <value>
    /// The color code for the player (from the View). 
    /// </value>
    public string ColorName { get; set; }
    /// <summary>
    /// Gets or sets the number of the player.
    /// </summary>
    public int Number { get; set; }
    /// <summary>
    /// Gets or sets the number of bonus armies for the player.
    /// </summary>
    public int ArmyBonus { get; set; }
    /// <summary>
    /// Gets or sets the number of armies in the pool for the player.
    /// </summary>
    public int ArmyPool { get; set; }
    /// <summary>
    /// Gets or sets the number of armies owned by the player.
    /// </summary>
    public int NumArmies { get; set; }
    /// <summary>
    /// Gets or sets the number of territories controlled by the player.
    /// </summary>
    public int NumTerritories { get; set; }
    /// <summary>
    /// Gets or sets the territories controlled by the player.
    /// </summary>
    public ObservableCollection<TerrID> Realm { get; set; }
    /// <summary>
    /// Gets or sets the continents controlled by the player.
    /// </summary>
    public ObservableCollection<ContID> Continents { get; set; }
    /// <summary>
    /// Gets or sets the names of the continents controlled by the player.
    /// </summary>
    public ObservableCollection<string> ContinentNames { get; set; }
    /// <summary>
    /// Gets or sets the cards held by the player.
    /// </summary>
    public ObservableCollection<ICardInfo> Hand { get; set; }
}
