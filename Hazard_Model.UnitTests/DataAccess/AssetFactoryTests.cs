using Hazard_Model.DataAccess;
using Hazard_Model.Tests.DataAccess.Mocks;
using Hazard_Model.Tests.Entities.Mocks;
using Hazard_Model.Tests.Fixtures;
using Hazard_Model.Tests.Fixtures.Stubs;
using Hazard_Share.Enums;
using Hazard_Share.Interfaces.Model;
using Hazard_Share.Services.Registry;
using Microsoft.Extensions.Logging;
using Microsoft.Testing.Platform.Logging;

namespace Hazard_Model.Tests.DataAccess;

[TestClass]
public class AssetFactoryTests
{
    private readonly MockDataFiles _mockFiles = new();
    private readonly LoggerStubT<AssetFactory> _logger = new();
    private readonly LoggerFactory _loggerFactory = new LoggerFactory();
    private readonly IDataProvider? _dataProvider;

    public AssetFactory TestFactory { get; private set; }

    public AssetFactoryTests()
    {
        _dataProvider = new MockDataProvider(_mockFiles.ConfigDataFileList);
        TestFactory = new(_dataProvider, _logger, _loggerFactory);
    }

    [TestMethod]
    public void GetAsset_TypeNameRegisteredForMockCard_ReturnMockCardSet()
    {
        string? testName = SharedRegister.Registry[typeof(MockCard)]![RegistryRelation.Name] as string;
        Assert.IsNotNull(testName);

        var returnedObjects = TestFactory.GetAsset(testName);

        Assert.IsTrue(returnedObjects is MockCardSet);

        var castObjects = (MockCardSet)returnedObjects;
        Assert.IsNotNull(castObjects.Cards);
        Assert.IsTrue(castObjects.Cards.Length == 50);
        Assert.IsTrue(castObjects.Cards.Where(card => Enum.IsDefined(typeof(TerrID), (int)card.Target[0])).Count() == 42); // MockTerrID has a Count of 50, so there are 8 undefined when cast to TerrID
    }
}
