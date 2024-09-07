using Hazard_Model.Entities;
using Hazard_Model.Tests.DataAccess.Mocks;
using Hazard_Model.Tests.Entities.Mocks;
using Hazard_Share.Interfaces.Model;
using Microsoft.Extensions.Logging;

namespace Hazard_Model.Tests.Core.Mocks;

public class MockCardBase : CardBase
{
    private readonly MockCardSet mockSet = new();
    private readonly MockCardSetData mockData = new();
    public MockCardBase(ILogger _logger) : base(_logger)
    {

        Sets = [];
        mockData.BuildFromMockData();
        mockSet.JData = mockData;
        Sets.Add(mockSet);
        int numMockCards = mockData.Targets.Length;
        if (numMockCards != mockData.Insignia.Length)
            throw new Exception($"{mockData} returned improper data.");
        List<MockCard> mockCards = [];
        for (int i = 0; i < numMockCards; i++) {
            mockCards.Add(new(mockSet) { Target = mockData.Targets[i], Insigne = mockData.Insignia[i] });
        }
        List<ICard> cards = [];
        cards.AddRange(mockCards);
        Initialize(Sets, cards);
    }

    public void Reset()
    {
        Sets = [];
        GameDeck = new();
    }
}
