using Share.Interfaces.Model;

namespace Model.Tests.Entities.Mocks;

public class MockCardFactory
{
    public ICard BuildCard(string typeName)
    {
        return new MockCard();
    }
}
