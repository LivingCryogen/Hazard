using Shared.Geography.Enums;
using Shared.Interfaces.Model;

namespace Model.Entities.Cards;
/// <summary>
/// A '.json' conversion data holder for <see cref="TroopCardSet"/>
/// </summary>
/// <inheritdoc cref="ICardSetData{T}"/>
public interface ITroopCardSetData<T> : ICardSetData<T> where T: struct, Enum
{
    /// <summary>
    /// Gets or sets insignia values for each <see cref="ITroopCard{T}"/> in a <see cref="TroopCardSet"/>.
    /// </summary>
    TroopInsignia[] Insignia { get; set; }
}
