using Microsoft.Extensions.Logging;
using Model.DataAccess;
using Model.Tests.DataAccess.Mocks;
using Model.Tests.Entities.Mocks;
using Model.Tests.Fixtures;
using Model.Tests.Fixtures.Stubs;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Registry;

namespace Model.Tests.DataAccess;

[TestClass]
public class AssetFactoryTests
{
    private readonly MockDataFiles _mockFiles = new();
    private readonly LoggerStubT<AssetFactory> _logger = new();
    private readonly LoggerFactory _loggerFactory = new();
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
        Assert.IsTrue(castObjects.Cards.Count == 50);
        Assert.IsTrue(castObjects.Cards.Where(card => Enum.IsDefined(typeof(TerrID), (int)card.Target[0])).Count() == 42); // MockTerrID has a Count of 50, so there are 8 undefined when cast to TerrID
    }
}
