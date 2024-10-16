﻿namespace Hazard_Share.Interfaces.Model;

/// <summary>
/// Data and methods for a collection of specified <see cref="ICard"/>s.
/// </summary>
/// <remarks>
/// <see cref="ICardSet"/> is initialized in two steps: <br/>
/// (1) By <see cref="Hazard_Model.DataAccess.Cards.ICardSetDataJConverter"/>. See also <see cref="ICardSetData"/> and <see cref="IDataProvider"/>.
/// (2) By <see cref="Hazard_Model.DataAccess.AssetFactory.BuildTroopCards(ICardSet)"/>.
/// </remarks>
public interface ICardSet
{
    /// <summary>
    /// Gets the name of the card set.
    /// </summary>
    /// <value>
    /// A string.
    /// </value>
    public string Name { get; }
    /// <summary>
    /// Gets the '.json' data object for this card set.
    /// </summary>
    /// <remarks>
    /// This is provided by the DAL in new games, but 
    /// </remarks>
    /// <value>
    /// An <see cref="ICardSetData"/> instance if the <see cref="ICardSet"/> has been loaded by the DAL; otherwise, <see langword="null"/>.
    /// </value>
    public ICardSetData? JData { get; }
    /// <summary>
    /// Gets the name of the <see cref="Type"/> which is the intended member of this collection.
    /// <br/> E.g. "TroopCard", see <see cref="Hazard_Model.Entities.Cards.TroopCard"/>.
    /// </summary>
    /// <remarks>
    /// Relationships between <see cref="ICard"/>s, <see cref="ICardSet"/>, and <see cref="ICardSetData"/> are established by <see cref="Services.Registry.ITypeRegister{T}"/>, which gets initial values from <see cref="Services.Registry.RegistryInitializer"/>.
    /// </remarks>
    /// <value>
    /// A string.
    /// </value>
    public string MemberTypeName { get; }
    /// <summary>
    /// Gets or sets the cards in this card set.
    /// </summary>
    /// <value>
    /// A list of <see cref="ICard"/>.
    /// </value>
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
    /// <param name="cards">An array of <see cref="ICard"/>s in which to search for matching trade-sets.</param>
    /// <returns>An array of <see cref="ICard"/>[] containing each valid trade-set found within <paramref name="cards"/>.</returns>
    public ICard[][]? FindTradeSets(ICard[] cards);
    /// <summary>
    /// Determines whether a group of cards is a valid set for trade-in.
    /// </summary>
    /// <param name="cards">An array of <see cref="ICard"/>s to test.</param>
    /// <returns><see langword="true"/> if the collection <paramref name="cards"/> qualifies exactly as a valid trade-set. Otherwise, <see langword="false"/>.</returns>
    public bool IsValidTrade(ICard[] cards);
}