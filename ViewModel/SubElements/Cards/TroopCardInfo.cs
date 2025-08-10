using Shared.Geography.Enums;
using Shared.Interfaces;
using Shared.Interfaces.Model;
using Shared.Interfaces.ViewModel;
using ViewModel.Services;

namespace ViewModel.SubElements.Cards;

public struct TroopCardInfo : ICardInfo, ITroopCardInfo<TerrID, ContID>
{
    public TroopCardInfo(ITroopCard troopCard)
    {
        BaseInfo = new(troopCard);
        InsigniaName = troopCard.Insigne.ToString();
        InsigniaValue = Convert.ToInt32(troopCard.Insigne);
        if (BaseInfo.TargetTerritory[0] == TerrID.Null && BaseInfo.TargetTerritory.Length == 1)
            DisplayName = "Wild";
        else
            DisplayName = DisplayNameBuilder.MakeDisplayName(BaseInfo.TargetTerritory[0].ToString());
    }

    public TroopCardInfo(ITroopCard troopCard, int owner, int ownerHandIndex)
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
    readonly int? ICardInfo.Owner { get => BaseInfo.Owner; }
    readonly int? ICardInfo.OwnerHandIndex { get => BaseInfo.OwnerHandIndex; }
    readonly TerrID[] ICardInfo.TargetTerritory { get => BaseInfo.TargetTerritory; }
    readonly ContID[] ICardInfo.TargetContinent { get => BaseInfo.TargetContinent; }
    public string DisplayName { get; init; }
    public string InsigniaName { get; set; }
    public int InsigniaValue { get; set; }
}
