using Model.Entities.Cards;
using Model.Tests.Entities.Mocks;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;

namespace Model.Tests.DataAccess.Mocks;

public class MockCardSetData : ITroopCardSetData
{
    public MockCard.Insignia[] Insignia { get; set; } = [];
    public TerrID[][] Targets { get; set; } = [];
    TroopInsignia[] ITroopCardSetData.Insignia { get => [.. Insignia.Select(insigne => (TroopInsignia)((int)insigne))]; set { } }

    public void BuildFromMockData()
    {
        MockFileData mockData = new();
        Insignia = mockData.Insignia;
        Targets = mockData.Targets;
    }
}
