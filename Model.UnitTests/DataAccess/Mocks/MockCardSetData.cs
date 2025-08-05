using Model.Entities.Cards;
using Model.Tests.Entities.Mocks;
using Model.Tests.Fixtures.Mocks;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;

namespace Model.Tests.DataAccess.Mocks;

public class MockCardSetData : ITroopCardSetData<MockTerrID>
{
    public MockCard.Insignia[] Insignia { get; set; } = [];
    public MockTerrID[][] Targets { get; set; } = [];
    TroopInsignia[] ITroopCardSetData<MockTerrID>.Insignia { get => Insignia.Select(insigne => ((TroopInsignia)((int)insigne))).ToArray(); set { } }
    MockTerrID[][] ICardSetData<MockTerrID>.Targets
    {
        get
        {
            List<MockTerrID[]> castList = [];
            foreach (MockTerrID[] targetList in Targets)
            {
                List<MockTerrID> innerCastList = [];
                foreach (MockTerrID id in targetList)
                    innerCastList.Add((MockTerrID)(int)id);

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
