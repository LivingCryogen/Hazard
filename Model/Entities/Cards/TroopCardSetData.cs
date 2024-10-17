using Share.Enums;

namespace Model.Entities.Cards;
/// <inheritdoc cref="ITroopCardSetData"/>
public class TroopCardSetData : ITroopCardSetData
{
    /// <inheritdoc cref="ITroopCardSetData.Insignia"/>
    public TroopInsignia[] Insignia { get; set; } = [];
    /// <inheritdoc cref="Share.Interfaces.Model.ICardSetData.Targets"/>
    public TerrID[][] Targets { get; set; } = [];
}
