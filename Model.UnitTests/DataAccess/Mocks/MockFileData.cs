using Model.Tests.Entities.Mocks;
using Model.Tests.Fixtures.Mocks;

namespace Model.Tests.DataAccess.Mocks;

public class MockFileData
{
    public MockFileData()
    {
        List<MockCard.Insignia> newInsignia = [];
        int enumCounter = 0;
        for (int i = 0; i < 50; i++) {
            newInsignia.Add((MockCard.Insignia)enumCounter);
            enumCounter++;
            if (enumCounter > 2)
                enumCounter = 0;
        }

        Insignia = [.. newInsignia];
    }

    public MockCard.Insignia[] Insignia { get; set; }

    public MockTerrID[][] Targets { get; set; } =
        [[MockTerrID.Alabama], [MockTerrID.Alaska], [MockTerrID.Arizona], [MockTerrID.Arkansas], [MockTerrID.California], [MockTerrID.Colorado], [MockTerrID.Connecticut], [MockTerrID.Delaware], [MockTerrID.Florida],
        [MockTerrID.Georgia], [MockTerrID.Hawaii], [MockTerrID.Idaho], [MockTerrID.Illinois], [MockTerrID.Indiana], [MockTerrID.Iowa], [MockTerrID.Kansas], [MockTerrID.Kentucky], [MockTerrID.Louisiana], [MockTerrID.Maine],
        [MockTerrID.Maryland], [MockTerrID.Massachusetts], [MockTerrID.Michigan], [MockTerrID.Minnesota], [MockTerrID.Mississippi], [MockTerrID.Missouri], [MockTerrID.Montana], [MockTerrID.Nebraska],
        [MockTerrID.Nevada], [MockTerrID.NewHampshire], [MockTerrID.NewJersey], [MockTerrID.NewMexico], [MockTerrID.NewYork], [MockTerrID.NorthCarolina], [MockTerrID.NorthDakota], [MockTerrID.Ohio],
        [MockTerrID.Oklahoma], [MockTerrID.Oregon], [MockTerrID.Pennsylvania], [MockTerrID.RhodeIsland], [MockTerrID.SouthCarolina], [MockTerrID.SouthDakota], [MockTerrID.Tennessee],
        [MockTerrID.Texas], [MockTerrID.Utah], [MockTerrID.Vermont], [MockTerrID.Virginia], [MockTerrID.Washington], [MockTerrID.WestVirginia], [MockTerrID.Wisconsin], [MockTerrID.Wyoming]];
}
