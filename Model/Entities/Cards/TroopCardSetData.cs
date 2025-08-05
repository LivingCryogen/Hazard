using Shared.Geography.Enums;

namespace Model.Entities.Cards;
/// <inheritdoc cref="ITroopCardSetData"/>
public class TroopCardSetData : ITroopCardSetData<TerrID>
{
    /// <inheritdoc cref="ITroopCardSetData.Insignia"/>
    public TroopInsignia[] Insignia { get; set; } = [];
    /// <inheritdoc cref="Shared.Interfaces.Model.ICardSetData.Targets"/>
    public TerrID[][] Targets { get; set; } = [];
}
