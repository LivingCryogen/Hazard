using Hazard_Share.Interfaces.Model;

namespace Hazard_Model.Tests.Entities.Mocks;

public class MockCardFactory
{
    public ICard BuildCard(string typeName)
    {
        return new MockCard();
    }
}
