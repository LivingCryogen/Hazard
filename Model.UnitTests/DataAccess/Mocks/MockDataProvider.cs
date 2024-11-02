using Model.Tests.Entities.Mocks;
using Shared.Interfaces.Model;

namespace Model.Tests.DataAccess.Mocks;

public class MockDataProvider(string[] dataFileNames) : IDataProvider
{
    public string[] DataFileNames { get; private set; } = dataFileNames;

    public object? GetData(string typeName)
    {
        if (typeName == "MockCard") {
            var set = new MockCardSet { JData = new MockCardSetData() };
            ((MockCardSetData)(set.JData)).BuildFromMockData();
            return set;
        }
        else return null;
    }
}
