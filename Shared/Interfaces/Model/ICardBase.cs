using Model.Entities;
using Model.Entities.Cards;
using Shared.Geography.Enums;
using Shared.Services.Serializer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Interfaces.Model;
/// <summary>
/// Encapsulates all objects primarily using <see cref="ICard{T}"/>s.
/// </summary>
/// <remarks>
/// E.g. <see cref="IDeck{T}"/> and <see cref="ICardSet{T}"/>s.
/// </remarks>
public interface ICardBase<T> : IBinarySerializable where T: struct, Enum
{
    /// <summary>
    /// Gets the factory used to create <see cref="ICard{T}"/>s.
    /// </summary>
    ICardFactory<T> CardFactory { get; }
    /// <summary>
    /// Gets or sets the deck of <see cref="ICard{T}"/> used in the game.
    /// </summary>
    IDeck<T> GameDeck { get; set; }
    /// <summary>
    /// Gets or sets the list of <see cref="ICardSet{T}"/>s used in the game."/>
    /// </summary>
    List<ICardSet<T>> Sets { get; set; }
    /// <summary>
    /// Initializes a discard pile when loading the game from a save file.
    /// </summary>
    /// <param name="cards">The discard pile's cards built during <see cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>.</param>
    void InitializeDiscardPile(ICard<T>[] cards);
    /// <summary>
    /// Initializes a cardbase with assets provided by <see cref="IAssetFetcher{T}"/>.
    /// </summary>
    /// <remarks>
    /// When a new game is started, the <see cref="CardBase"/> will include all <see cref="ICard{T}"/>s that can be found and converted from 'CardSet.json' files <br/>
    /// (see <see cref="IAssetFetcher{T}.FetchCardSets"/>, and <see cref="IAssetFactory.GetAsset(string)"/>). 
    /// <br/> Then, if <paramref name="defaultMode"/> is set to true, only <see cref="ITroopCard{T}"/>s will be retained.
    /// </remarks>
    /// <param name="assetFetcher">Gets initialized assets (objects loaded from data files) for specific Model properties.</param>
    /// <param name="defaultMode">A <see langwod="boolean"/> flag to indicate whether the <see cref="IGame{T, U}"/> is in default card mode.</param>
    void InitializeFromAssets(IAssetFetcher<T> assetFetcher, bool defaultMode);
    /// <summary>
    /// Initializes a library when loading the game from a save file.
    /// </summary>
    /// <param name="cards">The library's cards built during <see cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>.</param>
    void InitializeLibrary(ICard<T>[] cards);
    /// <summary>
    /// Ensures cards and card sets are properly mapped.
    /// </summary>
    /// <remarks>
    /// Necessary since application and/or game logic may depend on <see cref="ICard{T}.CardSet"/> (e.g. <see cref="ICardSet{T}.IsValidTrade(ICard{T}[])"/>).
    /// </remarks>
    /// <param name="cards">The cards whose sets must be discovered and mapped to.</param>
    void MapCardsToSets(ICard<T>[] cards);
}
