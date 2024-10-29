using Shared.Geography.Enums;
using System.Collections.ObjectModel;

namespace Shared.Interfaces.ViewModel;
/// <summary>
/// Defines public data for ViewModels that present <see cref="Model.IPlayer"/> data.
/// </summary>
public interface IPlayerData
{
    /// <summary>
    /// Gets or sets the <see cref="string">name</see> of this <see cref="IPlayerData"/>.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> derived from <see cref="Model.IPlayer.Name"/>.
    /// </value>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="string">color name</see> of this <see cref="IPlayerData"/>.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> derived from the color code for the player (from the View).
    /// </value>
    public string ColorName { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="int">number</see> of this <see cref="IPlayerData"/>.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> derived from <see cref="Model.IPlayer.Number"/>.
    /// </value>
    public int Number { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="int">number</see> of bonus armies for this <see cref="IPlayerData"/>.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> derived from <see cref="Model.IPlayer.ArmyBonus"/>.
    /// </value>
    public int ArmyBonus { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="int">number</see> of armies in the pool for this <see cref="IPlayerData"/>.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> derived from <see cref="Model.IPlayer.ArmyPool"/>.
    /// </value>
    public int ArmyPool { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="int">number</see> of armies for this <see cref="IPlayerData"/>.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> derived from <see cref="IMainVM.SumArmies(int)"/>.
    /// </value>
    public int NumArmies { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="int">number</see> of territories for this <see cref="IPlayerData"/>.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> derived from <see cref="Model.IPlayer.ControlledTerritories"/>.
    /// </value>
    public int NumTerritories { get; set; }
    /// <summary>
    /// Gets or sets a collection of <see cref="TerrID"/> representing the territories controlled by the <see cref="Model.IPlayer"/>.
    /// </summary>
    /// <value>
    /// An <see cref="ObservableCollection{T}"/> of <see cref="TerrID"/> corresponding to the values of <see cref="Model.IPlayer.ControlledTerritories"/>.
    /// </value>
    public ObservableCollection<TerrID> Realm { get; set; }
    /// <summary>
    /// Gets or sets a collection of <see cref="ContID"/> representing the continents controlled by the <see cref="Model.IPlayer"/>.
    /// </summary>
    /// <value>
    /// An <see cref="ObservableCollection{T}"/> of <see cref="ContID"/> corresponding to values in <see cref="Model.IBoard.ContinentOwner"/>.
    /// </value>
    public ObservableCollection<ContID> Continents { get; set; }
    /// <summary>
    /// Gets or sets a collection of <see cref="string">names</see> of the continents controlled by the <see cref="Model.IPlayer"/>.
    /// </summary>
    /// <value>
    /// An <see cref="ObservableCollection{T}"/> of <see cref="string"/> derived from <see cref="IMainVM.ContNameMap"/>.
    /// </value>
    public ObservableCollection<string> ContinentNames { get; set; }
    /// <summary>
    /// Gets or sets a collection of <see cref="ICardInfo"/> representing the <see cref="Model.ICard"/>s held by the <see cref="Model.IPlayer"/>.
    /// </summary>
    /// <value>
    /// An <see cref="ObservableCollection{T}"/> of <see cref="ICardInfo"/>, each derived from an <see cref="Model.ICard"/> in <see cref="Model.IPlayer.Hand"/>.
    /// </value>
    public ObservableCollection<ICardInfo> Hand { get; set; }
}
