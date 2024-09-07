using Hazard_Share.Enums;

namespace Hazard_Model.Entities.Cards;
/// <inheritdoc cref="ITroopCardSetData"/>
public class TroopCardSetData : ITroopCardSetData
{
    /// <inheritdoc cref="ITroopCardSetData.Insignia"/>
    public TroopInsignia[] Insignia { get; set; } = [];
    /// <inheritdoc cref="Hazard_Share.Interfaces.Model.ICardSetData.Targets"/>
    public TerrID[][] Targets { get; set; } = [];
}
