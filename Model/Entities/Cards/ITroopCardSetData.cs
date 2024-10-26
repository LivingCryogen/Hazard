using Share.Interfaces.Model;

namespace Model.Entities.Cards;
/// <summary>
/// A '.json' conversion data holder for <see cref="TroopCardSet"/>
/// </summary>
/// <inheritdoc cref="ICardSetData"/>
public interface ITroopCardSetData : ICardSetData
{
    /// <summary>
    /// Gets or sets insignia values, obtained from <see cref="IDataProvider"/>, for each <see cref="ITroopCard"/> in a <see cref="TroopCardSet"/>.
    /// </summary>
    TroopInsignia[] Insignia { get; set; }
}
