using Shared.Geography.Enums;
using Shared.Interfaces.Model;

namespace Model.Entities.Cards;
/// <summary>
/// A '.json' conversion data holder for <see cref="TroopCardSet"/>
/// </summary>
/// <inheritdoc cref="ICardSetData"/>
public interface ITroopCardSetData : ICardSetData 
{
    /// <summary>
    /// Gets or sets insignia values for each <see cref="ITroopCard"/> in a <see cref="TroopCardSet"/>.
    /// </summary>
    TroopInsignia[] Insignia { get; set; }
}
