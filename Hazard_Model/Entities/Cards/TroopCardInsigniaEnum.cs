namespace Hazard_Model.Entities.Cards;
/// <summary>
/// The insignia types for the default cards of the base game: <see cref="TroopCard"/>.
/// </summary>
/// <remarks>
/// Matching a set of these allows players to turn their <see cref="TroopCard"/>s in for bonus armies. Each TroopCard face displays its insignia.
/// </remarks>
public enum TroopInsignia
{
    /// <summary>
    /// Serves as any insignia.
    /// </summary>
    /// <remarks>
    /// The default card set requires sets of three cards in two possible combinations: 3 identical insignia, or 3 different insignia. <br/>
    /// A single wild will guarantee a three card set within five cards. For details, see <see cref="Hazard_Model.Entities.Cards.TroopCardSet.IsValidTrade(Hazard_Share.Interfaces.Model.ICard[])"/>.
    /// </remarks>
    Wild = 0,
    /// <summary>
    /// Denotes that its <see cref="TroopCard"/> displays the 'Soldier' insignia/image.
    /// </summary>
    Soldier = 1,
    /// <summary>
    /// Denotes that its <see cref="TroopCard"/> displays the 'Cavalry' insignia/image.
    /// </summary>
    Cavalry = 2,
    /// <summary>
    /// Denotes that its <see cref="TroopCard"/> displays the 'Artillery' insignia/image.
    /// </summary>
    Artillery = 3
}
