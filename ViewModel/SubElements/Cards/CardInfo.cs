﻿using Shared.Geography;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Interfaces.ViewModel;

namespace ViewModel.SubElements.Cards;

public readonly struct CardInfo : ICardInfo<TerrID, ContID>
{
    public CardInfo(ICard<TerrID> card)
    {
        TargetTerritory = new TerrID[card.Target.Length];
        card.Target.CopyTo(TargetTerritory, 0);
        TargetContinent = TargetTerritory.Select(terr => BoardGeography.TerritoryToContinent(terr))
            .Distinct()
            .ToArray();
        Owner = null;
        OwnerHandIndex = null;
    }

    public CardInfo(int owner, int ownerHandIndex)
    {
        Owner = owner;
        OwnerHandIndex = ownerHandIndex;
        TargetTerritory = [];
        TargetContinent = [];
    }

    public CardInfo(ICard<TerrID> card, int owner, int ownerHandIndex)
    {
        TargetTerritory = new TerrID[card.Target.Length];
        card.Target.CopyTo(TargetTerritory, 0);
        TargetContinent = TargetTerritory.Select(terr => BoardGeography.TerritoryToContinent(terr))
            .Distinct()
            .ToArray();
        Owner = owner;
        OwnerHandIndex = ownerHandIndex;
    }

    public CardInfo(string territory, string continent, int owner, int ownerHandIndex)
    {
        TargetTerritory = [Enum.Parse<TerrID>(territory)];
        TargetContinent = [Enum.Parse<ContID>(continent)];
        Owner = owner;
        OwnerHandIndex = ownerHandIndex;
    }

    public int? Owner { get; init; }
    public int? OwnerHandIndex { get; init; }
    public TerrID[] TargetTerritory { get; init; }
    public ContID[] TargetContinent { get; init; }
}
