using Hazard_Model.Entities;
using Hazard_Model.Tests.DataAccess.Mocks;
using Hazard_Share.Interfaces.Model;
using Hazard_Share.Services.Registry;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Hazard_Model.Tests.Entities.Mocks;

public class MockCardBase : CardBase
{
    private readonly MockCardSet mockSet = new();
    private readonly MockCardSetData mockData = new();
    public MockCardBase(ILogger logger, ITypeRegister<ITypeRelations> registry) : base(logger, registry)
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

    new public MockCardFactory CardFactory { get; set; } = new();

    public void Reset()
    {
        Sets = [];
        GameDeck = new();
    }
}
