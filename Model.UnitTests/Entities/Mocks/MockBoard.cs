using Microsoft.Extensions.Logging;
using Model.Tests.Fixtures.Stubs;
using Share.Enums;
using Share.Interfaces;
using Share.Interfaces.Model;
using Share.Services.Serializer;

namespace Model.Tests.Entities.Mocks;

internal class MockBoard : IBoard
{
    private ILogger<MockBoard> _logger = new LoggerStubT<MockBoard>();
    public List<object> this[int playerNumber, string enumName] => throw new NotImplementedException();

    public MockBoard()
    {
        Armies = [];
        Armies.Add(TerrID.Alaska, 10);
        TerritoryOwner = [];
        TerritoryOwner.Add(TerrID.Alaska, 1);
        ContinentOwner = [];
        ContinentOwner.Add(ContID.NorthAmerica, -1);
    }

    public IGeography Geography => new MockGeography();

    public Dictionary<TerrID, int> Armies { get; init; }
    public Dictionary<TerrID, int> TerritoryOwner { get; init; }
    public Dictionary<ContID, int> ContinentOwner { get; init; }

#pragma warning disable CS0414 // For unit-testing, these are unused. If integration tests are built, they should be, at which time these warnings should be re-enabled.
    public event EventHandler<ITerritoryChangedEventArgs>? TerritoryChanged = null;
    public event EventHandler<IContinentOwnerChangedEventArgs>? ContinentOwnerChanged = null;
#pragma warning restore CS0414

    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(() =>
        {
            int numCont = ContinentOwner.Count;
            int numTerr = TerritoryOwner.Count;
            int numData = 2 + numCont + (numTerr * 2);
            SerializedData[] saveData = new SerializedData[numData];
            saveData[0] = new(typeof(int), [numCont]);
            saveData[1] = new(typeof(int), [numTerr]);
            int dataIndex = 2;
            for (int i = 0; i < numCont; i++) {
                saveData[dataIndex] = new(typeof(int), [ContinentOwner[(ContID)i]]);
                dataIndex++;
            }
            for (int i = 0; i < numTerr; i++) {
                saveData[dataIndex] = new(typeof(int), [TerritoryOwner[(TerrID)i]]);
                dataIndex++;
                saveData[dataIndex] = new(typeof(int), [Armies[(TerrID)i]]);
                dataIndex++;
            }

            return saveData;
        });
    }
    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try {
            int numCont = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            int numTerr = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            ContinentOwner.Clear();
            for (int i = 0; i < numCont; i++)
                ContinentOwner.Add((ContID)i, (int)BinarySerializer.ReadConvertible(reader, typeof(int)));
            TerritoryOwner.Clear();
            Armies.Clear();
            for (int i = 0; i < numTerr; i++) {
                TerritoryOwner.Add((TerrID)i, (int)BinarySerializer.ReadConvertible(reader, typeof(int)));
                Armies.Add((TerrID)i, (int)BinarySerializer.ReadConvertible(reader, typeof(int)));
            }
        } catch (Exception ex) {
            _logger.LogError("An exception was thrown while loading {EarthBoard}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
    public void CheckContinentFlip(TerrID changed, int previousOwner)
    {
        throw new NotImplementedException();
    }

    public void Claims(int newPlayer, TerrID territory)
    {
        throw new NotImplementedException();
    }

    public void Claims(int newPlayer, TerrID territory, int armies)
    {
        throw new NotImplementedException();
    }

    public void Conquer(TerrID source, TerrID target, int newOwner)
    {
        throw new NotImplementedException();
    }

    public void Reinforce(TerrID territory)
    {
        throw new NotImplementedException();
    }

    public void Reinforce(TerrID territory, int armies)
    {
        throw new NotImplementedException();
    }
}
