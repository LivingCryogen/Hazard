using Model.Entities;
using Share.Enums;
using Share.Interfaces.Model;
using Share.Interfaces.ViewModel;

namespace ViewModel.SubElements.Cards;

public readonly struct CardInfo : ICardInfo
{
    public CardInfo(ICard card)
    {
        TargetTerritory = new TerrID[card.Target.Length];
        card.Target.CopyTo(TargetTerritory, 0);
        List<ContID> continents = [];
        foreach (TerrID territory in TargetTerritory) {
            var continent = EarthBoard.EarthGeography.TerrIDToContinent(territory);
            continents.Add(continent);
        }
        TargetContinent = continents.Distinct().ToArray();
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

    public CardInfo(ICard card, int owner, int ownerHandIndex)
    {
        TargetTerritory = new TerrID[card.Target.Length];
        card.Target.CopyTo(TargetTerritory, 0);
        List<ContID> continents = [];
        foreach (TerrID territory in TargetTerritory) {
            var continent = EarthBoard.EarthGeography.TerrIDToContinent(territory);
            continents.Add(continent);
        }
        var filteredContinentList = continents.Distinct();
        TargetContinent = filteredContinentList.ToArray();
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
