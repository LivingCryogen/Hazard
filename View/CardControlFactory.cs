using Share.Interfaces;
using Share.Interfaces.ViewModel;
using System.Windows.Controls;

namespace View;

public class CardControlFactory(IMainVM viewModel)
{
    private readonly IMainVM _viewModel = viewModel;

    public UserControl GetCardControl(ICardInfo card)
    {
        Face newCardFace;
        if (card is ITroopCardInfo troopCard) {
            if (!string.IsNullOrEmpty(troopCard.InsigniaName)) {
                if (troopCard.InsigniaName.Equals("Wild"))
                    newCardFace = Face.Wild;
                else if (troopCard.InsigniaName.Equals("Soldier") || troopCard.InsigniaName.Equals("Cavalry") || troopCard.InsigniaName.Equals("Artillery"))
                    newCardFace = Face.Troop;
                else throw new ArgumentOutOfRangeException(nameof(card));
            }
            else throw new ArgumentOutOfRangeException(nameof(card));

            TroopCardControl newCard = new(_viewModel) {
                Content = card,
                CardFace = newCardFace,
                Owner = card.Owner ?? -1
            };

            newCard.Build(_viewModel);

            return newCard;
        }
        else throw new ArgumentOutOfRangeException(nameof(card));
    }
}
