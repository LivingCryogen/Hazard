﻿using Hazard_Model.DataAccess;
using Hazard_Model.Entities;
using Hazard_Model.Tests.DataAccess.Mocks;
using Hazard_Model.Tests.Entities.Mocks;
using Hazard_Model.Tests.Fixtures;
using Hazard_Model.Tests.Fixtures.Mocks;
using Hazard_Model.Tests.Fixtures.Stubs;
using Hazard_Share.Interfaces.Model;
using Hazard_Share.Services.Registry;
using Microsoft.Extensions.Logging;

namespace Hazard_Model.Tests.DataAccess;

[TestClass]
public class DataProviderTests
{
    private readonly MockDataFiles _mockFiles = new();
    ILogger<DataProvider> Logger { get; } = new LoggerStub<DataProvider>();
    private readonly DataProvider? _testDataProvider;
    private readonly string[]? _configDataFiles;

    public DataProviderTests()
    {
        _configDataFiles = _mockFiles.ConfigDataFileList;
        _testDataProvider = new(_configDataFiles, SharedRegister.Registry, Logger);
    }

    [TestMethod]
    public void GetData_TypeNotRegistered_ReturnNull()
    {
        string typeName = "TestType";

        var data = _testDataProvider!.GetData(typeName);

        Assert.IsNull(data);
    }
    [TestMethod]
    public void GetData_NoDataFileRelation_ThrowKeyNotFoundException()
    {
        try {
            var data = _testDataProvider!.GetData(nameof(SharedRegister));
        } catch (Exception ex) {
            Assert.IsInstanceOfType(ex, typeof(KeyNotFoundException));
        }
    }
    [TestMethod]
    public void GetData_RegisteredFileNameNotFoundInConfig_ThrowKeyNotFoundException()
    {
        try {
            var data = _testDataProvider!.GetData(nameof(EarthBoard));
        } catch (Exception ex) {
            Assert.IsInstanceOfType(ex, typeof(KeyNotFoundException));
        }
    }
    [TestMethod]
    public void GetData_NoDataConverter_ThrowKeyNotFoundException()
    {
        try {
            var data = _testDataProvider!.GetData(nameof(ICard));
        } catch (Exception ex) {
            Assert.IsInstanceOfType(ex, typeof(KeyNotFoundException));
        }
    }
    [TestMethod]
    public void GetData_NoConvertedDataTypeDefault_ConvertedDataTypeReturnsSameType()
    {
        try {
            var data = _testDataProvider!.GetData(nameof(ICard));
        } catch (Exception ex) {
            Assert.IsInstanceOfType(ex, typeof(KeyNotFoundException));
        }
    }
    [TestMethod]
    public void GetData_NoConvertedDataTypeNoDefault_ThrowsKeyNotFoundException()
    {
        try {
            var data = _testDataProvider!.GetData(nameof(Deck));
        } catch (Exception ex) {
            Assert.IsInstanceOfType(ex, typeof(KeyNotFoundException));
        }
    }
    [TestMethod]
    public void GetData_RegisteredNameMockCard_ReturnMockCardSet()
    {
        string registeredName = (string)SharedRegister.Registry[typeof(MockCard)]![RegistryRelation.Name]!;

        var returned = _testDataProvider!.GetData(registeredName);

        Assert.IsTrue(returned is MockCardSet);

        var returnedCardSetData = ((MockCardSet)returned).JData;

        Assert.IsNotNull(returnedCardSetData);
        Assert.IsTrue(returnedCardSetData.Targets.Length > 0);
        foreach (var targetList in returnedCardSetData.Targets)
            Assert.IsTrue(targetList.Length > 0);
        foreach (MockTerrID mockID in Enum.GetValues(typeof(MockTerrID))) {
            var mockTargets = returnedCardSetData.Targets.SelectMany(array => array).Cast<MockTerrID>();
            Assert.IsTrue(mockTargets.Contains(mockID));
        }
    }
}