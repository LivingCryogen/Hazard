using Model.Entities.Cards;
using Model.Tests.Fixtures.Mocks;
using Shared.Interfaces.Model;

namespace Model.Tests.Entities.Mocks;

public class MockCardFactory(ICardSet<MockTerrID> mockSet) : ICardFactory<MockTerrID>
{
    private readonly ICardSet<MockTerrID> _mockSet = mockSet;

    public ICard<MockTerrID> BuildCard(string typeName)
    {
        return new MockCard(_mockSet);
    }
}
