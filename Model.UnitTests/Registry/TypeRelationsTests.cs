using Share.Services.Registry;

namespace Model.Tests.Registry;
[TestClass]
public class TypeRelationsTests
{
    private readonly struct TestStruct
    {
        public TestStruct()
        { }
        public object? Obj { get; } = null;
    }
    private TypeRelations? _testRelations;

    [TestInitialize]
    public void Setup()
    {
        _testRelations = new();
    }

    [TestMethod]
    public void IndexerRegistryRelationParam_RelationFound_ReturnsRelatedObject()
    {
        RegistryRelation testParam = RegistryRelation.ConvertedDataType;
        _testRelations!.Add(typeof(decimal), testParam);

        var indexerOutput = _testRelations[testParam];

        Assert.AreEqual(indexerOutput, typeof(decimal));
    }
    [TestMethod]
    public void IndexerRegistryRelationParam_RelationNotFound_ReturnsNull()
    {
        RegistryRelation testParam = RegistryRelation.ConvertedDataType;

        var indexerOutput = _testRelations![testParam];

        Assert.IsNull(indexerOutput);
    }
    [TestMethod]
    public void Add_NullObject_ThrowsNullException()
    {
        Assert.IsNotNull(_testRelations);
        RegistryRelation repeatRelation = RegistryRelation.Name;
        object testParam = "Test";

        try {
            _testRelations.Add(testParam, repeatRelation);
        } catch (Exception ex) {
            Assert.IsInstanceOfType(ex, typeof(ArgumentException));
        }
    }
    [TestMethod]
    public void Add_NameRelationIsString_AddsToDictionary()
    {
        Assert.IsNotNull(_testRelations);
        RegistryRelation testRelation = RegistryRelation.Name;
        object testParam = nameof(TypeRelationsTests);

        _testRelations.Add(testParam, testRelation);

        Assert.AreEqual(testParam, _testRelations[testRelation]);
    }
    [TestMethod]
    public void Add_NameRelationIsNotString_ThrowsArgumentException()
    {
        Assert.IsNotNull(_testRelations);
        RegistryRelation testRelation = RegistryRelation.Name;
        object testParam = 123;

        try {
            _testRelations.Add(testParam, testRelation);
        } catch (Exception ex) { Assert.IsInstanceOfType(ex, typeof(ArgumentException)); }
    }
    [TestMethod]
    public void Add_DataFileNameRelationIsString_AddsToDictionary()
    {
        Assert.IsNotNull(_testRelations);
        RegistryRelation testRelation = RegistryRelation.DataFileName;
        object testParam = "TestDataFile.json";

        _testRelations.Add(testParam, testRelation);

        Assert.AreEqual(testParam, _testRelations[testRelation]);
    }
    [TestMethod]
    public void Add_DataFileNameRelationIsNotString_ThrowsArgumentException()
    {
        Assert.IsNotNull(_testRelations);
        RegistryRelation testRelation = RegistryRelation.DataFileName;
        object testParam = 123;

        try {
            _testRelations.Add(testParam, testRelation);
        } catch (Exception ex) { Assert.IsInstanceOfType(ex, typeof(ArgumentException)); }
    }
    [TestMethod]
    public void Add_DataConverterIsClass_AddsToDictionary()
    {
        Assert.IsNotNull(_testRelations);
        RegistryRelation testRelation = RegistryRelation.DataConverter;
        object testObj = this;

        _testRelations.Add(testObj, testRelation);

        Assert.AreEqual(this, _testRelations[RegistryRelation.DataConverter]);
    }
    [TestMethod]
    public void Add_DataConverterIsNotClass_ThrowsArgumentException()
    {
        Assert.IsNotNull(_testRelations);
        RegistryRelation testRelation = RegistryRelation.DataConverter;
        object testObj = new TestStruct();

        try {
            _testRelations.Add(testObj, testRelation);
        } catch (Exception ex) {
            Assert.IsInstanceOfType(ex, typeof(ArgumentException));
        }
    }
    [TestMethod]
    public void Add_ConvertedDataTypeIsType_AddsToDictionary()
    {
        Assert.IsNotNull(_testRelations);
        RegistryRelation testRelation = RegistryRelation.ConvertedDataType;
        object testObj = GetType();

        _testRelations.Add(testObj, testRelation);

        Assert.AreEqual(GetType(), _testRelations[RegistryRelation.ConvertedDataType]);
    }
    [TestMethod]
    public void Add_ConvertedDataTypeIsNotType_ThrowsArgumentException()
    {
        Assert.IsNotNull(_testRelations);
        RegistryRelation testRelation = RegistryRelation.ConvertedDataType;
        object testObj = this;

        try {
            _testRelations.Add(testObj, testRelation);
        } catch (Exception ex) {
            Assert.IsInstanceOfType(ex, typeof(ArgumentException));
        }
    }
    [TestMethod]
    public void Remove_RelationFound_RemovesDictionaryEntry()
    {
        Assert.IsNotNull(_testRelations);
        RegistryRelation removeTestRelation = RegistryRelation.DataFileName;
        object testParam = "RemoveTest.json";

        _testRelations.Add(testParam, removeTestRelation);

        Assert.AreEqual(testParam, _testRelations[removeTestRelation]);

        _testRelations.Remove(removeTestRelation);

        Assert.IsNull(_testRelations[removeTestRelation]);
    }
    [TestMethod]
    public void Remove_RelationNotFound_ThrowsKeyNotFoundException()
    {
        Assert.IsNotNull(_testRelations);
        RegistryRelation removeTestRelation = RegistryRelation.Name;

        try {
            _testRelations.Remove(removeTestRelation);
        } catch (Exception ex) {
            Assert.IsInstanceOfType(ex, typeof(KeyNotFoundException));
        }
    }
}
