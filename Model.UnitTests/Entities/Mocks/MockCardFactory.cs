using Model.Entities.Cards;
using Shared.Interfaces.Model;

namespace Model.Tests.Entities.Mocks;

public class MockCardFactory(ICardSet mockSet) : ICardFactory
{
    private readonly ICardSet _mockSet = mockSet;

    public ICard BuildCard(string typeName)
    {
        return new MockCard(_mockSet);
    }
}
