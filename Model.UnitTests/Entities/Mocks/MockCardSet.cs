using Model.Tests.Fixtures.Mocks;
using Shared.Interfaces.Model;

namespace Model.Tests.Entities.Mocks;

public class MockCardSet : ICardSet<MockTerrID>
{
    public string TypeName { get; } = nameof(MockCardSet);
    public string MemberTypeName { get; } = nameof(MockCard);
    public ICardSetData<MockTerrID>? JData { get; set; } = null;
    public List<ICard<MockTerrID>> Cards { get; set; } = [];
    public bool ForcesTrade { get; } = true;

    public ICard<MockTerrID>[][]? FindTradeSets(ICard<MockTerrID>[] cards)
    {

        int matchNum = 3;
        var troopCards = cards.OfType<MockCard>();
        if (troopCards.Count() < matchNum)
            return null;

        List<ICard<MockTerrID>[]> tradeSets = [];

        for (int first = 0; first < cards.Length - matchNum; first++)
        {
            for (int second = first + 1; second < cards.Length - (matchNum - 1); second++)
            {
                for (int third = second + 1; third < cards.Length - (matchNum - 2); third++)
                {
                    ICard<MockTerrID>[] testCards = [cards[first], cards[second], cards[third]];
                    if (IsValidTrade(testCards))
                        tradeSets.Add(testCards);
                }
            }
        }

        if (tradeSets.Count > 0)
            return [.. tradeSets];
        else
            return null;
    }

    public bool IsValidTrade(ICard<MockTerrID>[] cards)
    {
        throw new NotImplementedException();
    }
}
