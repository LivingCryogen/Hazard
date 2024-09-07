using Hazard_Model.DataAccess.Cards;
using Hazard_Model.Tests.DataAccess.Mocks;
using Hazard_Model.Tests.Entities.Mocks;
using Hazard_Model.Tests.Fixtures.Mocks;

namespace Hazard_Model.Tests.DataAccess;

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
        foreach (MockTerrID mockID in Enum.GetValues(typeof(MockTerrID))) {
            var mockTargets = cardSetData.Targets.SelectMany(array => array).Cast<MockTerrID>();
            Assert.IsTrue(mockTargets.Contains(mockID));
        }
    }
}
