using Model.DataAccess.Cards;
using Model.Tests.DataAccess.Mocks;
using Model.Tests.Entities.Mocks;
using Model.Tests.Fixtures.Mocks;
using Shared.Geography.Enums;

namespace Model.Tests.DataAccess;

[TestClass]
public class TroopCardJConverterTests
{
    private readonly MockDataFiles _dataFiles = new();

    [TestMethod]
    public void ReadData_JsonValid_ReturnTroopCardSetData()
    {
        MockCardDataJConverter testConverter = new();
        var data = ((ICardSetDataJConverter)testConverter).ReadCardSetData(_dataFiles.CardSetPath!);

        Assert.IsInstanceOfType(data, typeof(MockCardSet));

        var cardSetData = ((MockCardSet)data).JData;
        Assert.IsNotNull(cardSetData);
        Assert.IsTrue(cardSetData.Targets.Length > 0);
        foreach (var targetList in cardSetData.Targets)
            Assert.IsTrue(targetList.Length > 0);
        foreach (TerrID mockID in Enum.GetValues(typeof(TerrID)))
        {
            var mockTargets = cardSetData.Targets.SelectMany(array => array).Cast<TerrID>();
            if (mockID != TerrID.Null)
                Assert.IsTrue(mockTargets.Contains(mockID));
        }
    }
}
