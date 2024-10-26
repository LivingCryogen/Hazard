using Model.DataAccess;
using Model.DataAccess.Cards;
using Model.Entities.Cards;

namespace Share.Interfaces.Model;


/// <summary>
/// Data and methods for a collection of specified <see cref="ICard"/>s.
/// </summary>
/// <remarks>
/// <see cref="ICardSet"/> is initialized in two steps: <br/>
/// (1) By <see cref="ICardSetDataJConverter"/>. See also <see cref="ICardSetData"/> and <see cref="IDataProvider"/>.
/// (2) By <see cref="AssetFactory.BuildTroopCards(ICardSet)"/>.
/// </remarks>
public interface ICardSet
{
    /// <summary>
    /// Gets the name of this card set's <see cref="Type"/>.
    /// </summary>
    /// <remarks>
    /// Serves as a cached value that allows us to avoid multiple reflection method calls (e.g.: .GetType()). 
    /// </remarks>
    public string TypeName { get; }
    /// <summary>
    /// Gets the '.json' data object for this card set.
    /// </summary>
    /// <remarks>
    /// This is provided by the DAL in new games, but will remain <see langword="null"/> when loading from a save file.
    /// </remarks>
    /// <value>
    /// An <see cref="ICardSetData"/> instance if the <see cref="ICardSet"/> has been loaded by the DAL; otherwise, <see langword="null"/>.
    /// </value>
    public ICardSetData? JData { get; }
    /// <summary>
    /// Gets the name of the <see cref="Type"/> which is the intended member of this collection.
    /// <br/> E.g. "TroopCard", see <see cref="TroopCard"/>.
    /// </summary>
    /// <remarks>
    /// Relationships between <see cref="ICard"/>s, <see cref="ICardSet"/>, and <see cref="ICardSetData"/> are established by <see cref="Services.Registry.ITypeRegister{T}"/>, which gets initial values from <see cref="Services.Registry.RegistryInitializer"/>.
    /// </remarks>
    public string MemberTypeName { get; }
    /// <summary>
    /// Gets or sets a list of cards in this card set.
    /// </summary>
    public List<ICard> Cards { get; set; }
    /// <summary>
    /// Gets a flag indicating if a trade should be forced when a matching set of <see cref="ICard"/>s from this <see cref="ICardSet"/> are obtained.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if obtaining matching cards (satisfying <see cref="IsValidTrade"/>) should force an <see cref="IPlayer"/> to trade (via <see cref="IRegulator.TradeInCards(int, int[])"/>).
    /// <br/> Otherwise, <see langword="false"/>.
    /// </value>
    public bool ForcesTrade { get; }
    /// <summary>
    /// Identifies any number of matching trade sets present in any <see cref="ICard"/>s.
    /// </summary>
    /// <param name="cards">An array of cards in which to search for matching trade-sets.</param>
    /// <returns>A staggered array of cards containing each valid trade-set found within <paramref name="cards"/>.</returns>
    public ICard[][]? FindTradeSets(ICard[] cards);
    /// <summary>
    /// Determines whether a group of cards is a valid set for trade-in.
    /// </summary>
    /// <param name="cards">An array of cards to test.</param>
    /// <returns><see langword="true"/> if the collection <paramref name="cards"/> qualifies exactly as a valid trade-set. Otherwise, <see langword="false"/>.</returns>
    public bool IsValidTrade(ICard[] cards);
    /// <summary>
    /// Validates a card as a member of this set. 
    /// </summary>
    /// <param name="card">The card that may be a member of this card set.</param>
    /// <returns><see langword="true"/> if <paramref name="card"/>'s relevant properties verify it belongs to this set; otherwise, <see langword="false"/>.</returns>
    public bool IsParent(ICard card)
    {
        if (MemberTypeName != card.TypeName)
            return false;
        if (card.ParentTypeName != TypeName)
            return false;

        return true;
    }
}