using Model.Entities.Cards;
using Model.Tests.DataAccess.Mocks;
using Model.Tests.Fixtures;
using Model.Tests.Fixtures.Mocks;
using Model.Tests.Registry.Stubs;
using Shared.Interfaces.Model;
using Shared.Services.Registry;

namespace Model.Tests.Registry;

[TestClass]
public class TypeRegisterTests
{
    private RegisterInitializerStub? _testRegistryInitializer;
    private TypeRegister? _testRegister;

    [TestInitialize]
    public void Setup()
    {
        _testRegistryInitializer = new();
        _testRegister = new(_testRegistryInitializer);

        TypeRelations dummyRelations = new([
            (nameof(TypeRegisterTests), RegistryRelation.Name),
            ("TypeRegisterTests.txt", RegistryRelation.DataFileName),
            (new MockCardDataJConverter(), RegistryRelation.DataConverter),
            (typeof(string), RegistryRelation.ConvertedDataType)
        ]);
        _testRegister.Register(typeof(TypeRegisterTests), dummyRelations);
    }

    [TestMethod]
    public void IndexerTypeNameParam_IsFound_ReturnsTestType()
    {
        var indexReturn = _testRegister!["TypeRegisterTests"];

        Assert.IsInstanceOfType(indexReturn, typeof(Type));
        Assert.AreEqual(indexReturn, GetType());
    }
    [TestMethod]
    public void IndexerTypeNameParam_NotFound_ReturnsNull()
    {
        var indexReturn = _testRegister!["asifbgaoirbg"];

        Assert.IsNull(indexReturn);
    }
    [TestMethod]
    public void IndexerRegistryRelationParam_RegistryRelationsFound_ReturnsRegisteredObjectLists()
    {
        Assert.IsNotNull(_testRegister);
        _testRegister.Register(typeof(SharedRegister));
        _testRegister.AddRelation(typeof(SharedRegister), ("Shared..txt", RegistryRelation.DataFileName));
        _testRegister.AddRelation(typeof(SharedRegister), (new MockCardDataJConverter(), RegistryRelation.DataConverter));

        var type = _testRegister[RegistryRelation.Name];
        var dataFileNameList = _testRegister[RegistryRelation.DataFileName];
        var dataConverterList = _testRegister[RegistryRelation.DataConverter];
        Assert.IsNotNull(type);
        Assert.IsNotNull(dataFileNameList);
        Assert.IsNotNull(dataConverterList);
        Assert.IsTrue(type.Select(item => item.RelatedObject).Where(item => item is string).Count() == 2);
        Assert.IsTrue(dataFileNameList.Select(item => item.RelatedObject).Where(item => item is string).Count() == 2);
        Assert.IsTrue(dataConverterList.Select(item => item.RelatedObject).Where(item => item is MockCardDataJConverter).Count() == 2);
    }
    [TestMethod]
    public void IndexerRegistryRelationParm_MultipleRelationsFound_ReturnsArrayOfEntries()
    {
        Assert.IsNotNull(_testRegister);

        TypeRelations icardRelations = new([("I C A R D", RegistryRelation.Name)]);
        _testRegister.Register(typeof(ICard), icardRelations);

        TypeRelations troopCardRelations = new([("I T R O O P", RegistryRelation.Name)]);
        _testRegister.Register(typeof(TroopCard), troopCardRelations);

        var indexOut = _testRegister[RegistryRelation.Name];
        Assert.IsNotNull(indexOut);
        Assert.AreEqual(indexOut.Length, 3);
        Assert.AreEqual(indexOut[0].KeyType, typeof(TypeRegisterTests));
        Assert.AreEqual(indexOut[1].KeyType, typeof(ICard));
        Assert.AreEqual(indexOut[2].KeyType, typeof(TroopCard));
        Assert.AreEqual(indexOut[0].RelatedObject, "TypeRegisterTests");
        Assert.AreEqual(indexOut[1].RelatedObject, "I C A R D");
        Assert.AreEqual(indexOut[2].RelatedObject, "I T R O O P");
    }
    [TestMethod]
    public void IndexerRegistryRelationParam_RelationNotFound_ReturnsNull()
    {
        Assert.IsNotNull(_testRegister);
        _testRegister.Clear();

        var convertedDataType = _testRegister[RegistryRelation.ConvertedDataType];

        Assert.IsNull(convertedDataType);
    }
    [TestMethod]
    public void Register_ProperTypeAndRelations_AddToRegistry()
    {
        Assert.IsNotNull(_testRegister);
        Type newType = typeof(TypeRegister);
        TypeRelations newRelations = new(
        [
            ("TypeRegister.txt", RegistryRelation.DataFileName),
            ("TypeRegister", RegistryRelation.Name)
        ]);

        _testRegister.Register(newType, newRelations);

        var outType = _testRegister["TypeRegister"];
        var outRelations = _testRegister[newType];
        var outEntry = _testRegister[RegistryRelation.DataFileName];
        Assert.IsNotNull(outEntry);
        Assert.AreEqual(outType, newType);
        Assert.AreEqual(outRelations, newRelations);
        Assert.AreEqual(outEntry[1].RelatedObject, "TypeRegister.txt");
    }
    [TestMethod]
    public void AddRelation_TypeAndRelationProper_AddToRegistry()
    {
        TypeRegister testRegister = new(new MockRegistryInitializer());
        testRegister.Register(GetType(), new TypeRelations([(nameof(TypeRegisterTests), RegistryRelation.Name)]));
        testRegister.AddRelation(GetType(), ("NewDataFile.json", RegistryRelation.DataFileName));

        var indexTypeNameOut = testRegister[nameof(TypeRegisterTests)];
        var indexTypeOut = testRegister[GetType()];
        var indexRelationOut = testRegister[RegistryRelation.DataFileName];

        Assert.AreEqual(GetType(), indexTypeNameOut);
        Assert.IsNotNull(indexTypeOut);
        Assert.IsNotNull(indexRelationOut);
        Assert.AreEqual(indexTypeOut[RegistryRelation.DataFileName], "NewDataFile.json");
        Assert.IsTrue(indexRelationOut.Select(item => item.RelatedObject).Contains("NewDataFile.json"));
    }
    [TestMethod]
    public void AddRelation_TypeUnregistered_ThrowArgumentException()
    {
        TypeRegister testRegister = new(new MockRegistryInitializer());
        testRegister.Register(GetType(), new TypeRelations([(nameof(TypeRegisterTests), RegistryRelation.Name)]));

        try
        {
            testRegister.AddRelation(typeof(TroopCard), ("TroopCard", RegistryRelation.Name));
        }
        catch (Exception ex)
        {
            Assert.IsInstanceOfType(ex, typeof(ArgumentException));
        }
    }
    [TestMethod]
    public void RemoveRelation_TypeAndRelationProper_RemovedFromRegistry()
    {
        var testRegister = new TypeRegister(new MockRegistryInitializer());
        testRegister.Register(GetType(), new TypeRelations([(GetType().Name, RegistryRelation.Name)]));

        testRegister.RemoveRelation(GetType(), RegistryRelation.Name);

        var indexOut = testRegister[GetType()];

        Assert.IsNull(indexOut);
    }
    [TestMethod]
    public void RemoveRelation_TypeNotFound_ThrowKeyNotFoundException()
    {
        var testRegister = new TypeRegister(new MockRegistryInitializer());
        testRegister.Register(GetType(), new TypeRelations([(GetType().Name, RegistryRelation.Name)]));

        try
        {
            testRegister.RemoveRelation(typeof(TroopCard), RegistryRelation.Name);
        }
        catch (Exception ex)
        {
            Assert.IsInstanceOfType(ex, typeof(KeyNotFoundException));
        }
    }
    [TestMethod]
    public void RemoveRelation_RelationNotFound_ThrowArgumentException()
    {
        var testRegister = new TypeRegister(new MockRegistryInitializer());
        testRegister.Register(GetType(), new TypeRelations([(GetType().Name, RegistryRelation.Name)]));

        try
        {
            testRegister.RemoveRelation(GetType(), RegistryRelation.DataFileName);
        }
        catch (Exception ex)
        {
            Assert.IsInstanceOfType(ex, typeof(ArgumentException));
        }
    }
    [TestMethod]
    public void DeRegister_TypeProper_RemoveFromRegistry()
    {
        var testRegister = new TypeRegister(new MockRegistryInitializer());
        var thisType = GetType();
        testRegister.Register(thisType, new TypeRelations([(thisType.Name, RegistryRelation.Name)]));

        testRegister.DeRegister(thisType);

        var stringIndexOut = testRegister[thisType];
        var typeIndexOut = testRegister[thisType];

        Assert.IsNull(stringIndexOut);
        Assert.IsNull(typeIndexOut);
    }
    [TestMethod]
    public void DeRegister_TypeNotFound_ThrowArgumentException()
    {
        var testRegister = new TypeRegister(new MockRegistryInitializer());
        var thisType = GetType();
        testRegister.Register(thisType, new TypeRelations([(thisType.Name, RegistryRelation.Name)]));

        try
        {
            testRegister.DeRegister(typeof(TroopCard));
        }
        catch (Exception ex)
        {
            Assert.IsInstanceOfType(ex, typeof(ArgumentException));
        }
    }
}
