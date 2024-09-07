namespace Hazard_Share.Interfaces.Model;
/// <summary><see cref="ICard"/> extension for the default card type included in the base game: <see cref="Hazard_Model.Entities.Cards.TroopCard"/>.</summary>
/// <inheritdoc cref="ICard"/>
public interface ITroopCard : ICard
{
    /// <summary>
    /// Gets or sets an insignia value for the card. 
    /// </summary>
    /// <value>
    /// An enum. For <see cref="Hazard_Model.Entities.Cards.TroopCard"/>, this is <see cref="Hazard_Model.Entities.Cards.TroopInsignia"/>.
    /// </value>
    Enum Insigne { get; set; }
}
