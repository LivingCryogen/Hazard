using Hazard_Model.Entities.Cards;
using Hazard_Share.Interfaces.Model;
using Hazard_Share.Services.Registry;
using Hazard_Share.Services.Serializer;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Hazard_Model.Entities;
/// <summary>
/// Encapsulates all objects primarily using <see cref="ICard"/>s.
/// </summary>
/// <remarks>
/// E.g. <see cref="GameDeck"/> and <see cref="Hazard_Model.Entities.Cards.TroopCardSet"/>.
/// </remarks>
/// <param name="logger">An <see cref="ILogger"/> for logging debug information and errors.</param>
public class CardBase(ILogger logger, ITypeRegister<ITypeRelations> registry) : IBinarySerializable
{
    private readonly ILogger _logger = logger;
    public CardFactory CardFactory { get; } = new(registry);
    /// <summary>
    /// Gets or sets the list of <see cref="ICardSet"/>s used in this <see cref="IGame"/>.
    /// </summary>
    /// <value>
    /// A <see cref="List{T}"/> if <see cref="CardBase"/>is initialized; otherwise, <see langword="null"/>.
    /// </value>
    public List<ICardSet> Sets { get; set; } = [];
    /// <summary>
    /// Gets or sets the deck of cards to be used for this game.
    /// </summary>
    /// <value>
    /// A <see cref="Deck"/> if <see cref="CardBase"/>is initialized; otherwise, <see langword="null"/>.
    /// </value>
    public Deck GameDeck { get; set; } = new();
    /// <summary>
    /// Initializes a <see cref="CardBase"/> with assets provided by <see cref="IAssetFetcher"/>.
    /// </summary>
    /// <remarks>
    /// When a new game is started, the <see cref="CardBase"/> will include all <see cref="ICard"/>s that can be found and converted from 'CardSet.json' files <br/>
    /// (see <see cref="IAssetFetcher.FetchCardSets"/>, and <see cref="IAssetFactory.GetAsset(string)"/>). 
    /// <br/> Then, if <paramref name="defaultMode"/> is set to true, only <see cref="ITroopCard"/>s will be retained.
    /// </remarks>
    /// <param name="assetFetcher">The application's sole <see cref="IAssetFetcher"/>.</param>
    /// <param name="defaultMode">A <see langwod="boolean"/> flag to indicate whether the <see cref="IGame"/> is in default card mode or not.</param>
    public void Initialize(IAssetFetcher assetFetcher, bool defaultMode) // defaultMode needs to be added to Game Serialization/Load
    {
        Sets = assetFetcher.FetchCardSets() ?? [];
        if (Sets == null || Sets.Count == 0)
            return;

        List<ICard> defaultCards = [];
        foreach (var set in Sets)
            if (set?.Cards?.Length > 0)
                if (set.Cards.OfType<ITroopCard>().Count() == set.Cards.Length) {
                    defaultCards.AddRange(set.Cards);
                    var setTypeName = set.GetType().Name;
                    foreach (ICard card in defaultCards)
                        if (card.ParentTypeName == setTypeName && set.MemberTypeName == card.GetType().Name && card.CardSet == null)
                            card.CardSet = set;
                }

        if (defaultMode)
            GameDeck = new(defaultCards.ToArray());
        else
            GameDeck = new(Sets.ToArray());

        GameDeck.Shuffle();
    }
    /// <summary>
    /// Initializes a <see cref="CardBase"/> with assets already provided (e.g. by <see cref="DataAccess.BinarySerializer"/>).
    /// </summary>
    /// <param name="sets">A list of necessary <see cref="ICardSet"/>s.</param>
    /// <param name="library">A list of <see cref="ICard"/>s representing the library of the game <see cref="Deck"/>.</param>
    /// <param name="discard">A list of <see cref="ICard"/>s representing the discard pile of the game <see cref="Deck"/>.</param>
    /// <param name="players">The list of <see cref="IPlayer"/>s participating in this <see cref="IGame"/>.</param>
    public void Initialize(List<ICardSet> sets, List<ICard> library, List<ICard> discard, List<IPlayer> players)
    {
        GameDeck = new() { Library = library, DiscardPile = discard };
        Dictionary<string, ICardSet> cardTypeNamesToCardSetsMap = [];
        Dictionary<ICardSet, List<ICard>?> cardSetsToCardListsMap = [];

        foreach (var set in sets) {
            if (!string.IsNullOrEmpty(set.MemberTypeName)) {
                cardTypeNamesToCardSetsMap.TryAdd(set.MemberTypeName, set);
                cardSetsToCardListsMap.TryAdd(set, []);
            }
        }

        MapCardsAndSets(GameDeck.Library, cardTypeNamesToCardSetsMap, cardSetsToCardListsMap);
        MapCardsAndSets(GameDeck.DiscardPile, cardTypeNamesToCardSetsMap, cardSetsToCardListsMap);
        foreach (IPlayer player in players)
            MapCardsAndSets(player.Hand, cardTypeNamesToCardSetsMap, cardSetsToCardListsMap);

        foreach (ICardSet set in cardSetsToCardListsMap.Keys) {
            var list = cardSetsToCardListsMap[set];
            if (list == null)
                set.Cards = null;
            else
                set.Cards = [.. list];
        }
        Sets = [.. cardSetsToCardListsMap.Keys];
    }
    /// <summary>
    /// Initializes a <see cref="CardBase"/> with only basic assets already provided.
    /// </summary>
    /// <remarks>
    /// Primarily used for certain unit tests.
    /// </remarks>
    /// <param name="sets">A list of necessary <see cref="ICardSet"/>s.</param>
    /// <param name="library">A list of all <see cref="ICard"/>s used; will begin within the library of the game <see cref="Deck"/>.</param>
    public void Initialize(List<ICardSet> sets, List<ICard> library)
    {
        Sets = sets;
        GameDeck = new() { Library = library, DiscardPile = [] };
        Dictionary<string, ICardSet> cardTypeNamesToCardSetsMap = [];
        Dictionary<ICardSet, List<ICard>?> cardSetsToCardListsMap = [];

        Sets ??= [];
        foreach (var set in Sets) {
            if (!string.IsNullOrEmpty(set.MemberTypeName)) {
                cardTypeNamesToCardSetsMap.TryAdd(set.MemberTypeName, set);
                cardSetsToCardListsMap.TryAdd(set, []);
            }
        }

        MapCardsAndSets(GameDeck.Library, cardTypeNamesToCardSetsMap, cardSetsToCardListsMap);
        MapCardsAndSets(GameDeck.DiscardPile, cardTypeNamesToCardSetsMap, cardSetsToCardListsMap);

        foreach (ICardSet set in cardSetsToCardListsMap.Keys) {
            var list = cardSetsToCardListsMap[set];
            if (list == null)
                set.Cards = null;
            else
                set.Cards = [.. list];
        }
        Sets = [.. cardSetsToCardListsMap.Keys];
    }
    private void MapCardsAndSets(List<ICard> cards, Dictionary<string, ICardSet> cardTypeNamesToSets, Dictionary<ICardSet, List<ICard>?> setsToCardLists)
    {
        foreach (ICard newCard in cards) {
            string cardTypeName = newCard.GetType().Name;
            ICardSet? parentSet = null;
            try {
                parentSet = cardTypeNamesToSets[cardTypeName];
            } catch (KeyNotFoundException e) {
                _logger.LogWarning("On loading, could not locate the ICardSet for {NewCard}. Full error: {Message}, {Data}, {Source}.", newCard, e.Message, e.Data, e.Source);
            }

            if (parentSet == null)
                _logger.LogWarning("During load of CardBase, {TypeNameToSetMap} returned a null value.", cardTypeNamesToSets);
            else
                newCard.CardSet = parentSet;

            List<ICard>? setList = null;
            if (parentSet != null) {
                try {
                    setList = setsToCardLists[parentSet];
                } catch (Exception e) {
                    _logger.LogWarning("On loading, could not locate the CardList in the {SetToCardListMap} for {ParentSet}. Full error: {Message}, {Data}, {Source}.", setsToCardLists, parentSet, e.Message, e.Data, e.Source);
                }
            }

            if (parentSet != null && setsToCardLists[parentSet] != null) {
                setList ??= [];
                setList.Add(newCard);
            }
        }
    }
    public async Task<SerializedData[]> GetBinarySerials()
    {
        if (GameDeck == null)
            return [];
        return await Task.Run(async () => {
            List<SerializedData> serials = [];
            int numLibrary = GameDeck.Library.Count;
            serials.Add(new(typeof(int), [numLibrary]));
            for (int i = 0; i < numLibrary; i++) {
                ICard currentCard = GameDeck.Library[i];
                serials.Add(new (typeof(string), [currentCard.TypeName]));
                IEnumerable<SerializedData> cardSerials = await currentCard.GetBinarySerials();
                serials.AddRange(cardSerials ?? []);
            }
            int numDiscard = GameDeck.DiscardPile.Count;
            serials.Add(new(typeof(int), [numDiscard]));
            for (int i = 0; i < numDiscard; i++) {
                ICard currentCard = GameDeck.DiscardPile[i];
                serials.Add(new(typeof(string), [currentCard.TypeName]));
                IEnumerable<SerializedData> cardSerials = await currentCard.GetBinarySerials();
                serials.AddRange(cardSerials ?? []);
            }

            return serials.ToArray();
        });
    }
    public bool LoadFromBinary(BinaryReader reader)
    {
        GameDeck.Library = [];
        GameDeck.DiscardPile = [];
        bool loadComplete = true;
        
        try {
            int numLibrary = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            for (int i = 0; i < numLibrary; i++) {
                string typeName = (string)BinarySerializer.ReadConvertible(reader, typeof(string));
                if (CardFactory.BuildCard(typeName) is not ICard newCard) {
                    _logger.LogWarning("{CardFactory} failed to construct a card of type {name} during loading of {base}.", CardFactory, typeName, this);
                    loadComplete = false;
                    continue;
                }
                newCard.LoadFromBinary(reader);
                GameDeck.Library.Add(newCard);
            }
            int numDiscard = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            GameDeck.DiscardPile = new(numDiscard);
            for (int i = 0; i < numDiscard; i++) {
                string typeName = (string)BinarySerializer.ReadConvertible(reader, typeof(string));
                if (CardFactory.BuildCard(typeName) is not ICard newCard) {
                    _logger.LogWarning("{CardFactory} failed to construct a card of type {name} during loading of {base}.", CardFactory, typeName, this);
                    loadComplete = false;
                    continue;
                }
                newCard.LoadFromBinary(reader);
                GameDeck.DiscardPile.Add(newCard);
            }

        } catch (Exception ex) {
            _logger.LogError("An exception was thrown while loading {Player}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
}
