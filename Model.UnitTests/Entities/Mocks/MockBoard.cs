using Microsoft.Extensions.Logging;
using Model.Tests.Fixtures.Stubs;
using Shared.Geography.Enums;
using Shared.Interfaces;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;

namespace Model.Tests.Entities.Mocks;

internal class MockBoard : IBoard<MockTerrID, MockContID>
{
    private readonly ILogger<MockBoard> _logger = new LoggerStubT<MockBoard>();
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

    public Dictionary<TerrID, int> Armies { get; init; }
    public Dictionary<TerrID, int> TerritoryOwner { get; init; }
    public Dictionary<ContID, int> ContinentOwner { get; init; }

    Dictionary<TerrID, int> IBoard.Armies { get => Armies.ToDictionary(kvp => (TerrID)(int)(object)kvp.Key, kvp => kvp.Value); }
    Dictionary<TerrID, int> IBoard.TerritoryOwner { get => TerritoryOwner.ToDictionary(kvp => (TerrID)(int)(object)kvp.Key, kvp => kvp.Value); }
    Dictionary<ContID, int> IBoard.ContinentOwner { get => ContinentOwner.ToDictionary(kvp => (ContID)(int)(object)kvp.Key, kvp => kvp.Value); }

    Dictionary<TerrID, int> IBoard.Armies { get => Armies.ToDictionary(kvp => (TerrID)(int)(object)kvp.Key, kvp => kvp.Value); }
    Dictionary<TerrID, int> IBoard.TerritoryOwner { get => TerritoryOwner.ToDictionary(kvp => (TerrID)(int)(object)kvp.Key, kvp => kvp.Value); }
    Dictionary<ContID, int> IBoard.ContinentOwner { get => ContinentOwner.ToDictionary(kvp => (ContID)(int)(object)kvp.Key, kvp => kvp.Value); }

    Dictionary<TerrID, int> IBoard.Armies { get => Armies.ToDictionary(kvp => (TerrID)(int)(object)kvp.Key, kvp => kvp.Value); }
    Dictionary<TerrID, int> IBoard.TerritoryOwner { get => TerritoryOwner.ToDictionary(kvp => (TerrID)(int)(object)kvp.Key, kvp => kvp.Value); }
    Dictionary<ContID, int> IBoard.ContinentOwner { get => ContinentOwner.ToDictionary(kvp => (ContID)(int)(object)kvp.Key, kvp => kvp.Value); }

    Dictionary<TerrID, int> IBoard.Armies { get => Armies.ToDictionary(kvp => (TerrID)(int)(object)kvp.Key, kvp => kvp.Value); }
    Dictionary<TerrID, int> IBoard.TerritoryOwner { get => TerritoryOwner.ToDictionary(kvp => (TerrID)(int)(object)kvp.Key, kvp => kvp.Value); }
    Dictionary<ContID, int> IBoard.ContinentOwner { get => ContinentOwner.ToDictionary(kvp => (ContID)(int)(object)kvp.Key, kvp => kvp.Value); }

#pragma warning disable CS0414 // For unit-testing, these are unused. If integration tests are built, they should be, at which time these warnings should be re-enabled.
    public event EventHandler<ITerritoryChangedEventArgs<MockTerrID>>? TerritoryChanged = null;
    public event EventHandler<IContinentOwnerChangedEventArgs<MockContID>>? ContinentOwnerChanged = null;
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
            for (int i = 0; i < numCont; i++)
            {
                saveData[dataIndex] = new(typeof(int), [ContinentOwner[(ContID)i]]);
                dataIndex++;
            }
            for (int i = 0; i < numTerr; i++)
            {
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
        try
        {
            int numCont = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            int numTerr = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            ContinentOwner.Clear();
            for (int i = 0; i < numCont; i++)
    public void CheckContinentFlip(TerrID changed, int previousOwner)
            TerritoryOwner.Clear();
            Armies.Clear();
            for (int i = 0; i < numTerr; i++)
            {
                TerritoryOwner.Add((TerrID)i, (int)BinarySerializer.ReadConvertible(reader, typeof(int)));
                Armies.Add((TerrID)i, (int)BinarySerializer.ReadConvertible(reader, typeof(int)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("An exception was thrown while loading {EarthBoard}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    public void Conquer(TerrID source, TerrID target, int newOwner)
    public ContID? CheckContinentFlip(TerrID changed, int previousOwner)
    {
        throw new NotImplementedException();
    }

    public void Claims(int newPlayer, MockTerrID territory, int armies)
    {
        throw new NotImplementedException();
    }

    public void Conquer(MockTerrID source, MockTerrID target, int newOwner, out MockContID? flipped)
    {
        throw new NotImplementedException();
    }

    public void Conquer(TerrID source, TerrID target, int newOwner, out ContID? flipped)
    {
        throw new NotImplementedException();
    }

    public void Reinforce(MockTerrID territory, int armies)
    {
        throw new NotImplementedException();
    }

    MockContID? IBoard<MockTerrID, MockContID>.CheckContinentFlip(MockTerrID changed, int previousOwner)
    {
        throw new NotImplementedException();
    }
}
