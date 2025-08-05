using Shared.Geography.Enums;
using Shared.Interfaces.Model;

namespace ViewModel.SubElements.Cards;

public class CardInfoFactory()
{
    public static object BuildCardInfo(ICard<TerrID> card, int owner, int ownerHandIndex)
    {
        if (card is ITroopCard<TerrID> troopCard)
        {
            return new TroopCardInfo(troopCard, owner, ownerHandIndex);
        }
        return new CardInfo(card, owner, ownerHandIndex);
    }
}
