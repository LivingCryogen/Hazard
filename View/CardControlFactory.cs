﻿using Shared.Geography.Enums;
using Shared.Interfaces;
using Shared.Interfaces.ViewModel;
using System.Windows.Controls;

namespace View;

public class CardControlFactory(IMainVM<TerrID, ContID> viewModel)
{
    private readonly IMainVM<TerrID, ContID> _viewModel = viewModel;

    public UserControl GetCardControl(ICardInfo<TerrID, ContID> card)
    {
        Face newCardFace;
        if (card is ITroopCardInfo<TerrID, ContID> troopCard)
        {
            if (string.IsNullOrEmpty(troopCard.InsigniaName))
                throw new ArgumentOutOfRangeException(nameof(card));

            if (troopCard.InsigniaName == "Wild")
                newCardFace = Face.Wild;
            else if (troopCard.InsigniaName == "Soldier" || troopCard.InsigniaName == "Cavalry" || troopCard.InsigniaName == "Artillery")
                newCardFace = Face.Troop;
            else throw new ArgumentOutOfRangeException(nameof(card));

            TroopCardControl newCard = new(_viewModel)
            {
                Content = card,
                CardFace = newCardFace,
                Owner = card.Owner ?? -1
            };

            newCard.Build(_viewModel);

            return newCard;
        }

        // extensions to ICard beyond ITroopCard must add their CardControl creation logic here

        throw new ArgumentOutOfRangeException(nameof(card));
    }
}
