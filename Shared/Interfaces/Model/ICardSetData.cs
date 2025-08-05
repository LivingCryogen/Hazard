using Model.Entities.Cards;
using Shared.Geography.Enums;

namespace Shared.Interfaces.Model;
/// <summary>
/// A base interface for '.json' conversion classes for <see cref="ICardSet"/>s. 
/// </summary>
/// <remarks>
/// <see cref="ICardSet"/> intialization requires: (1) '.json' data from <see cref="IDataProvider"/>, and (2) <see cref="$1ICard{T}$2"/>s from <see cref="IAssetFactory"/> or <see cref="CardFactory"/>.<br/>
/// <see cref="ICardSetData"/> implementations store the data from step (1). See <see cref="ICardSet.JData"/>. <br/>
/// For an example, see: <see cref="TroopCardSetData"/>.
/// </remarks>
public interface ICardSetData<T> where T : struct, Enum
{
    /// <summary>
    /// Gets or sets the values for each <see cref="$1ICard{T}$2"/> as read from the '.json' for their <see cref="ICardSet"/>.
    /// </summary>
    /// <value>
    /// A staggered array of territory targets; each <see cref="$1ICard{T}$2"/> will be set to one <see cref="TerrID"/> array.
    /// </value>
    T[][] Targets { get; set; }
}
