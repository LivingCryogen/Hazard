using Shared.Geography.Enums;
using Shared.Interfaces;
using Shared.Interfaces.Model;
using Shared.Interfaces.ViewModel;
using ViewModel.Services;

namespace ViewModel.SubElements.Cards;

public struct TroopCardInfo : ICardInfo<TerrID, ContID>, ITroopCardInfo<TerrID, ContID>
{
    public TroopCardInfo(ITroopCard<TerrID> troopCard)
    {
        BaseInfo = new(troopCard);
        InsigniaName = troopCard.Insigne.ToString();
        InsigniaValue = Convert.ToInt32(troopCard.Insigne);
        if (BaseInfo.TargetTerritory[0] == TerrID.Null && BaseInfo.TargetTerritory.Length == 1)
            DisplayName = "Wild";
        else
            DisplayName = DisplayNameBuilder.MakeDisplayName(BaseInfo.TargetTerritory[0].ToString());
    }

    public TroopCardInfo(ITroopCard<TerrID> troopCard, int owner, int ownerHandIndex)
    {
        BaseInfo = new(troopCard, owner, ownerHandIndex);
        InsigniaName = troopCard.Insigne.ToString();
        InsigniaValue = Convert.ToInt32(troopCard.Insigne);
        if (BaseInfo.TargetTerritory[0] == TerrID.Null && BaseInfo.TargetTerritory.Length == 1)
            DisplayName = "Wild";
        else
            DisplayName = DisplayNameBuilder.MakeDisplayName(BaseInfo.TargetTerritory[0].ToString());
    }
    public CardInfo BaseInfo { get; init; }
    readonly int? ICardInfo<TerrID, ContID>.Owner { get => BaseInfo.Owner; }
    readonly int? ICardInfo<TerrID, ContID>.OwnerHandIndex { get => BaseInfo.OwnerHandIndex; }
    readonly TerrID[] ICardInfo<TerrID, ContID>.TargetTerritory { get => BaseInfo.TargetTerritory; }
    readonly ContID[] ICardInfo<TerrID, ContID>.TargetContinent { get => BaseInfo.TargetContinent; }
    public string DisplayName { get; init; }
    public string InsigniaName { get; set; }
    public int InsigniaValue { get; set; }
}
