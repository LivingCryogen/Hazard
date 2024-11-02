using Microsoft.Extensions.Logging;
using Model.Entities;
using Model.Tests.DataAccess.Mocks;
using Shared.Services.Registry;

namespace Model.Tests.Entities.Mocks;

public class MockCardBase : CardBase
{
    private readonly MockCardSetData mockData = new();
    public MockCardBase(ITypeRegister<ITypeRelations> registry) : base(new LoggerFactory(), registry)
    {
        Sets = [];
        mockData.BuildFromMockData();
        MockCardSet mockSet = new() { JData = mockData };
        int numMockCards = mockData.Targets.Length;
        if (numMockCards != mockData.Insignia.Length)
            throw new Exception($"{mockData} returned improper data.");
        List<MockCard> mockCards = [];
        for (int i = 0; i < numMockCards; i++) {
            MockCard newMock = new(mockSet) { Target = mockData.Targets[i], Insigne = mockData.Insignia[i] };
            newMock.FillTestValues();
            mockCards.Add(newMock);
        }
        mockSet.Cards = [.. mockCards];
        Sets.Add(mockSet);
        GameDeck.Library.AddRange(mockCards);
    }

    new public MockCardFactory CardFactory { get; set; } = new();
    public void Wipe()
    {
        Sets.Clear();
        GameDeck.Library.Clear();
        GameDeck.DiscardPile.Clear();
    }
}
