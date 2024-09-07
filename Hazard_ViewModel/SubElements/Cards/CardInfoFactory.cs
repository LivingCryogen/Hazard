using Hazard_Share.Interfaces.Model;

namespace Hazard_ViewModel.SubElements.Cards;

public class CardInfoFactory()
{
    public static object BuildCardInfo(ICard card, int owner, int ownerHandIndex)
    {
        if (card is ITroopCard troopCard) {
            return new TroopCardInfo(troopCard, owner, ownerHandIndex);
        }
        return new CardInfo(card, owner, ownerHandIndex);
    }
}
