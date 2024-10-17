using Share.Interfaces.Model;

namespace Model.Entities.Cards;
/// <summary>
/// The card set for the game's default card type, <see cref="TroopCard"/>.
/// </summary>
/// <inheritdoc cref="ICardSet"/>
public class TroopCardSet : ICardSet
{
    /// <inheritdoc cref="ICardSet.Name"/>
    public string Name { get; } = nameof(TroopCardSet);
    /// <inheritdoc cref="ICardSet.JData"/>
    public ITroopCardSetData? JData { get; set; } = null;
    ICardSetData? ICardSet.JData { get => (ICardSetData?)JData; }
    /// <inheritdoc cref="ICardSet.MemberTypeName"/>
    public string MemberTypeName { get; } = nameof(TroopCard);
    /// <inheritdoc cref="ICardSet.Cards"/>
    public List<ICard> Cards { get; set; } = [];
    /// <inheritdoc cref="ICardSet.ForcesTrade"/>
    public bool ForcesTrade { get; } = true;

    /// <remarks>
    /// This method solves the 'combination generation' problem; the included solution is brute-force and should be replaced with standard <br/>
    /// optimal solutions if 'n' (number of cards) or 'k' (number in a match) gets large.
    /// </remarks>
    /// <inheritdoc cref="ICardSet.FindTradeSets(ICard[])"/>
    public ICard[][]? FindTradeSets(ICard[] cards)
    {
        int matchNum = 3;
        if (cards.Length < matchNum)
            return null;
        var troopCards = cards.OfType<TroopCard>();
        if (troopCards.Count() < matchNum)
            return null;

        List<ICard[]> tradeSets = [];

        for (int first = 0; first <= cards.Length - matchNum; first++) {
            for (int second = first + 1; second <= cards.Length - (matchNum - 1); second++) {
                for (int third = second + 1; third <= cards.Length - (matchNum - 2); third++) {
                    ICard[] testCards = [cards[first], cards[second], cards[third]];
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
    /// <remarks>
    /// This default set of <see cref="TroopCard"/> stipulates a matching set of <see cref="ICard"/>: <br/>
    /// (1) contains 3 of its cards <br/>
    /// (2) contains only tradeble cards (see <see cref="ICard.IsTradeable"/>) <br/>
    /// (3) contains <see cref="TroopCard"/>s with all identical or all different <see cref="TroopInsignia"/> (after wilds).
    /// </remarks>
    /// <inheritdoc cref="ICardSet.IsValidTrade(ICard[])"/>
    public bool IsValidTrade(ICard[] cards)
    {
        if (cards.Length != 3)
            return false;

        var troopCards = cards.OfType<TroopCard>();
        if (troopCards.Count() != 3)
            return false;

        List<int> insigniaVal = [];
        foreach (var troopCard in troopCards) {
            if (troopCard.IsTradeable == false)
                return false;
            insigniaVal.Add((int)troopCard.Insigne);
        }

        if (IsTradeableSet([.. insigniaVal]))
            return true;

        return false;
    }
    private static bool IsTradeableSet(int[] insigniaValues)
    {
        if (insigniaValues.Length != 3) return false;
        // every combination of 3 cards with any Wilds is a valid Trade
        if (insigniaValues[0] == (int)TroopInsignia.Wild || insigniaValues[1] == (int)TroopInsignia.Wild || insigniaValues[2] == (int)TroopInsignia.Wild) return true;
        int totalValue = insigniaValues.Sum();
        // 1 of each Insignia is covered by the second test: 1 + 2 + 3 = 3 * 2
        if (totalValue == 3 * (int)TroopInsignia.Soldier || totalValue == 3 * (int)TroopInsignia.Cavalry || totalValue == 3 * (int)TroopInsignia.Artillery) return true;
        return false;
    }
}
