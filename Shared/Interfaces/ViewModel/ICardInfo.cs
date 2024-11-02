using Shared.Geography.Enums;

namespace Shared.Interfaces.ViewModel;
/// <summary>
/// Defines public data for ViewModel structs representing <see cref="Model.ICard"/>s.
/// </summary>
public interface ICardInfo
{
    /// <summary>
    /// Gets the owner of the card.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> representing the <see cref="Model.IPlayer.Number"/> who owns the <see cref="Model.ICard"/> represented by this <see cref="ICardInfo"/>. <br/>
    /// If it doesn't have one, or a dependent object is uninitialized, <see langword="null"/>.
    /// </value>
    public int? Owner { get; }
    /// <summary>
    /// Gets the index of the card's place in its owner's <see cref="Model.IPlayer.Hand"/>.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> representing the index of <see cref="Model.IPlayer.Hand"/> containing the <see cref="Model.ICard"/> represented by this <see cref="ICardInfo"/>. <br/>
    /// If there isn't one, or a dependent object is uninitialized, <see langword="null"/>.
    /// </value>
    public int? OwnerHandIndex { get; }
    /// <summary>
    /// Gets the territory ID's targeted by the card.
    /// </summary>
    /// <value>
    /// An array of <see cref="TerrID"/>.
    /// </value>
    public TerrID[] TargetTerritory { get; }
    /// <summary>
    /// Gets the continent ID's targeted by the card.
    /// </summary>
    /// <value>
    /// An array of <see cref="ContID"/>.
    /// </value>
    public ContID[] TargetContinent { get; }
}
