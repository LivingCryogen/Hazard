using Shared.Geography.Enums;

namespace Shared.Interfaces.ViewModel;
/// <summary>
/// Defines public data for ViewModel structs representing <see cref="$1ICard{T}$2"/>s.
/// </summary>
public interface ICardInfo<T, U> where T : struct, Enum where U : struct, Enum
{
    /// <summary>
    /// Gets the owner of the card.
    /// </summary>
    /// <value>
    /// The <see cref="Model.IPlayer.Number"/> who owns the card. <br/>
    /// If it doesn't have one, or a dependent object is uninitialized, <see langword="null"/>.
    /// </value>
    public int? Owner { get; }
    /// <summary>
    /// Gets the index of the card's place in its owner's <see cref="Model.IPlayer.Hand"/>.
    /// </summary>
    /// <value>
    /// The index of <see cref="Model.IPlayer.Hand"/> containing the card. <br/>
    /// If there isn't one, or a dependent object is uninitialized, <see langword="null"/>.
    /// </value>
    public int? OwnerHandIndex { get; }
    /// <summary>
    /// Gets the territories targeted by the card.
    /// </summary>
    public T[] TargetTerritory { get; }
    /// <summary>
    /// Gets the continents targeted by the card.
    /// </summary>
    public U[] TargetContinent { get; }
}
