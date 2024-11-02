using Model.Entities.Cards;
using Shared.Geography.Enums;

namespace Shared.Interfaces.Model;
/// <summary>
/// A base interface for '.json' conversion classes for <see cref="ICardSet"/>s. 
/// </summary>
/// <remarks>
/// <see cref="ICardSet"/> intialization requires: (1) '.json' data from <see cref="IDataProvider"/>, and (2) <see cref="ICard"/> instances from <see cref="IAssetFactory"/>.<br/>
/// <see cref="ICardSetData"/> implementations store the data from step (1). See <see cref="ICardSet.JData"/>. <br/>
/// For an example, see: <see cref="TroopCardSetData"/>.
/// </remarks>
public interface ICardSetData
{
    /// <summary>
    /// Gets or sets values for each <see cref="ICard.Target"/> as read from the '.json' for their <see cref="ICardSet"/>.
    /// </summary>
    /// <value>
    /// A staggered array of territory targets; each <see cref="ICard.Target"/> will be set to one <see cref="TerrID"/> array.
    /// </value>
    TerrID[][] Targets { get; set; }
}
