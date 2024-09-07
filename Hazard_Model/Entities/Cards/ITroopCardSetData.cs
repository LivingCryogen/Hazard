using Hazard_Share.Interfaces.Model;

namespace Hazard_Model.Entities.Cards;
/// <summary>
/// A '.json' conversion data holder for <see cref="TroopCardSet"/>
/// </summary>
/// <inheritdoc cref="ICardSetData"/>
public interface ITroopCardSetData : ICardSetData
{
    /// <summary>
    /// Gets or sets insignia values, obtained from <see cref="IDataProvider"/>, for each <see cref="ITroopCard"/> in a <see cref="TroopCardSet"/>.
    /// </summary>
    /// <value>
    /// An array of <see cref="TroopInsignia"/> -- one for each <see cref="ITroopCard"/> in the <see cref="TroopCardSet"/>.
    /// </value>
    TroopInsignia[] Insignia { get; set; }
}
