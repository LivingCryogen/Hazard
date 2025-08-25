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
    private readonly int numTerritories = 42;

    public AssetFactory TestFactory { get; private set; }

    public AssetFactoryTests()
    {
        Dictionary<string, string> mockDataMap = [];
        mockDataMap.Add(Path.GetFileName(_mockFiles.ConfigDataFileList[0]), _mockFiles.ConfigDataFileList[0]);
        _dataProvider = new MockDataProvider(mockDataMap);
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
        Assert.AreEqual(numTerritories, castObjects.Cards.Count);
        Assert.AreEqual(numTerritories, castObjects.Cards.Where(card => Enum.IsDefined(typeof(TerrID), (int)card.Target[0])).Count());
    }
}
