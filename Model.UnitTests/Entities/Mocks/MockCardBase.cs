using Microsoft.Extensions.Logging;
using Model.Entities;
using Model.Entities.Cards;
using Model.Tests.DataAccess.Mocks;
using Model.Tests.Fixtures.Mocks;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Registry;
using Shared.Services.Serializer;

namespace Model.Tests.Entities.Mocks;

public class MockCardBase : ICardBase<MockTerrID>
{
    private readonly MockCardSetData mockData = new();
    public MockCardBase(ITypeRegister<ITypeRelations> registry)
    {
        Sets = [];
        mockData.BuildFromMockData();
        MockCardSet mockSet = new() { JData = mockData };
        int numMockCards = mockData.Targets.Length;
        if (numMockCards != mockData.Insignia.Length)
            throw new Exception($"{mockData} returned improper data.");
        List<MockCard> mockCards = [];
        for (int i = 0; i < numMockCards; i++)
        {
            MockCard newMock = new(mockSet) { Target = mockData.Targets[i], Insigne = mockData.Insignia[i] };
            newMock.FillTestValues();
            mockCards.Add(newMock);
        }
        mockSet.Cards = [.. mockCards];
        Sets.Add(mockSet);
        GameDeck.Library.AddRange(mockCards);
        CardFactory = new MockCardFactory(mockSet);
    }

    public ICardFactory<MockTerrID> CardFactory { get; set; }
    public IDeck<MockTerrID> GameDeck { get; set; } = new MockDeck();
    public List<ICardSet<MockTerrID>> Sets { get; set; } = [];


    public Task<SerializedData[]> GetBinarySerials()
    {
        throw new NotImplementedException();
    }

    public void InitializeDiscardPile(ICard<MockTerrID>[] cards)
    {
        throw new NotImplementedException();
    }

    public void InitializeFromAssets(IAssetFetcher<MockTerrID> assetFetcher, bool defaultMode)
    {
        throw new NotImplementedException();
    }

    public void InitializeLibrary(ICard<MockTerrID>[] cards)
    {
        throw new NotImplementedException();
    }

    public bool LoadFromBinary(BinaryReader reader)
    {
        throw new NotImplementedException();
    }

    public void MapCardsToSets(ICard<MockTerrID>[] cards)
    {
        throw new NotImplementedException();
    }

    public void Wipe()
    {
        Sets.Clear();
        GameDeck.Library.Clear();
        GameDeck.DiscardPile.Clear();
    }

    void ICardBase<MockTerrID>.InitializeDiscardPile(ICard<MockTerrID>[] cards)
    {
        throw new NotImplementedException();
    }

    void ICardBase<MockTerrID>.InitializeFromAssets(IAssetFetcher<MockTerrID> assetFetcher, bool defaultMode)
    {
        throw new NotImplementedException();
    }

    void ICardBase<MockTerrID>.InitializeLibrary(ICard<MockTerrID>[] cards)
    {
        throw new NotImplementedException();
    }

    void ICardBase<MockTerrID>.MapCardsToSets(ICard<MockTerrID>[] cards)
    {
        throw new NotImplementedException();
    }

    bool IBinarySerializable.LoadFromBinary(BinaryReader reader)
    {
        throw new NotImplementedException();
    }

    Task<SerializedData[]> IBinarySerializable.GetBinarySerials()
    {
        throw new NotImplementedException();
    }
}
