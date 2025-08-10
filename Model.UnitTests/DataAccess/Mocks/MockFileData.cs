using Model.Tests.Entities.Mocks;
using Model.Tests.Fixtures.Mocks;
using Shared.Geography.Enums;

namespace Model.Tests.DataAccess.Mocks;

public class MockFileData
{
    public MockFileData()
    {
        List<MockCard.Insignia> newInsignia = [];
        int enumCounter = 0;
        for (int i = 0; i < 50; i++)
        {
            newInsignia.Add((MockCard.Insignia)enumCounter);
            enumCounter++;
            if (enumCounter > 2)
                enumCounter = 0;
        }

        Insignia = [.. newInsignia];
    }

    public MockCard.Insignia[] Insignia { get; set; }

    public TerrID[][] Targets { get; set; } = [.. Enum.GetValues<TerrID>().Select(t => new[] { t })];
}
