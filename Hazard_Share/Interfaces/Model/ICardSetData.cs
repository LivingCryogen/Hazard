using Hazard_Share.Enums;

namespace Hazard_Share.Interfaces.Model;
/// <summary>
/// A base interface for '.json' conversion classes for <see cref="ICardSet"/>s. 
/// </summary>
/// <remarks>
/// <see cref="ICardSet"/> intialization requires: (1) '.json' data from <see cref="IDataProvider"/>, and (2) <see cref="ICard"/> instances from <see cref="IAssetFactory"/>.
/// <br/> <see cref="ICardSetData"/> implementations store the data from step (1). See <see cref="ICardSet.JData"/>. 
/// <br/> For an example, see: <see cref="Hazard_Model.Entities.Cards.TroopCardSetData"/>.
/// </remarks>
public interface ICardSetData
{
    /// <summary>
    /// Gets or sets values for each <see cref="ICard.Target"/> as read from the '.json' for their <see cref="ICardSet"/>.
    /// </summary>
    /// <value>
    /// A staggered array of <see cref="TerrID"/>; each <see cref="ICard.Target"/> gets one <see cref="TerrID"/> array.
    /// </value>
    TerrID[][] Targets { get; set; }
}
