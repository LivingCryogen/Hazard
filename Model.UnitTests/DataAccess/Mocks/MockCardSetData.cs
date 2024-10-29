using Model.Entities.Cards;
using Model.Tests.Entities.Mocks;
using Model.Tests.Fixtures.Mocks;
using Shared.Enums;
using Shared.Interfaces.Model;

namespace Model.Tests.DataAccess.Mocks;

public class MockCardSetData : ITroopCardSetData
{
    public MockCard.Insignia[] Insignia { get; set; } = [];
    public MockTerrID[][] Targets { get; set; } = [];
    TroopInsignia[] ITroopCardSetData.Insignia { get => Insignia.Select(insigne => ((TroopInsignia)((int)insigne))).ToArray(); set { } }
    TerrID[][] ICardSetData.Targets {
        get {
            List<TerrID[]> castList = [];
            foreach (MockTerrID[] targetList in Targets) {
                List<TerrID> innerCastList = [];
                foreach (MockTerrID id in targetList)
                    innerCastList.Add((TerrID)(int)id);

                castList.Add([.. innerCastList]);
            }
            return [.. castList];
        }
        set { }
    }

    public void BuildFromMockData()
    {
        MockFileData mockData = new();
        Insignia = mockData.Insignia;
        Targets = mockData.Targets;
    }
}
