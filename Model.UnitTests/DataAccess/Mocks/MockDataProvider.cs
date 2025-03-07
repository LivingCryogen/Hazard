using Model.Tests.Entities.Mocks;
using Shared.Interfaces.Model;
using System.Collections.ObjectModel;

namespace Model.Tests.DataAccess.Mocks;

public class MockDataProvider(Dictionary<string, string> dataFileMap) : IDataProvider
{
    public ReadOnlyDictionary<string, string> DataFileMap { get; private set; } = new(dataFileMap);

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
