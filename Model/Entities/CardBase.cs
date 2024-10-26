using Microsoft.Extensions.Logging;
using Model.Entities.Cards;
using Share.Interfaces.Model;
using Share.Services.Registry;
using Share.Services.Serializer;

namespace Model.Entities;
/// <summary>
/// Encapsulates all objects primarily using <see cref="ICard"/>s.
/// </summary>
/// <remarks>
/// E.g. <see cref="GameDeck"/> and <see cref="ICardSet"/>s.
/// </remarks>
/// <param name="loggerFactory">Instantiates loggers for logging debug information and errors.</param>
/// <param name="registry">The application's type registry.</param>
public class CardBase(ILoggerFactory loggerFactory, ITypeRegister<ITypeRelations> registry) : IBinarySerializable
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<CardBase>();
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    /// <summary>
    /// Gets a factory for making <see cref="ICard"/>s.
    /// </summary>
    /// <remarks>
    /// Used when loading from a save file; see <see cref="LoadFromBinary"/>.
    /// </remarks>
    public CardFactory CardFactory { get; } = new(registry, loggerFactory);
    /// <summary>
    /// Gets or sets a list of card sets.
    /// </summary>
    public List<ICardSet> Sets { get; set; } = [];
    /// <summary>
    /// Gets or sets the deck of cards to be used for this game.
    /// </summary>
    public Deck GameDeck { get; set; } = new();
    /// <summary>
    /// Initializes a cardbase with assets provided by <see cref="IAssetFetcher"/>.
    /// </summary>
    /// <remarks>
    /// When a new game is started, the <see cref="CardBase"/> will include all <see cref="ICard"/>s that can be found and converted from 'CardSet.json' files <br/>
    /// (see <see cref="IAssetFetcher.FetchCardSets"/>, and <see cref="IAssetFactory.GetAsset(string)"/>). 
    /// <br/> Then, if <paramref name="defaultMode"/> is set to true, only <see cref="ITroopCard"/>s will be retained.
    /// </remarks>
    /// <param name="assetFetcher">Gets initialized assets (objects loaded from data files) for specific Model properties.</param>
    /// <param name="defaultMode">A <see langwod="boolean"/> flag to indicate whether the <see cref="IGame"/> is in default card mode.</param>
    public void InitializeFromAssets(IAssetFetcher assetFetcher, bool defaultMode)
    {
        Sets = assetFetcher.FetchCardSets();
        if (Sets.Count == 0)
            return;

        List<ICard> defaultCards = [];
        foreach (var set in Sets) {
            if (set.Cards.Count == 0)
                continue;
            if (set.Cards.OfType<ITroopCard>().Count() == set.Cards.Count) {
                defaultCards.AddRange(set.Cards);
                var setTypeName = set.GetType().Name;
                foreach (ICard card in defaultCards)
                    if (card.CardSet == null && set.IsParent(card))
                        card.CardSet = set;
            }
        }
        if (defaultMode)
            GameDeck = new(defaultCards.ToArray());
        else
            GameDeck = new(Sets.ToArray());

        GameDeck.Shuffle();
    }
    /// <summary>
    /// Initializes a library when loading the game from a save file.
    /// </summary>
    /// <param name="cards">The library's cards built during <see cref="LoadFromBinary(BinaryReader)"/>.</param>
    public void InitializeLibrary(ICard[] cards)
    {
        MapCardsToSets(cards);
        GameDeck.Library.AddRange(cards);
    }
    /// <summary>
    /// Initializes a discard pile when loading the game from a save file.
    /// </summary>
    /// <param name="cards">The discard pile's cards built during <see cref="LoadFromBinary(BinaryReader)"/>.</param>
    public void InitializeDiscardPile(ICard[] cards)
    {
        MapCardsToSets(cards);
        GameDeck.DiscardPile.AddRange(cards);
    }
    /// <summary>
    /// Ensures cards and card sets are properly mapped.
    /// </summary>
    /// <remarks>
    /// Necessary since application and/or game logic may depend on <see cref="ICard.CardSet"/> (e.g. <see cref="ICardSet.IsValidTrade(ICard[])"/>).
    /// </remarks>
    /// <param name="cards">The cards whose sets must be discovered and mapped to.</param>
    public void MapCardsToSets(ICard[] cards)
    {
        Sets ??= [];

        Dictionary<string, ICardSet> cardSetToTypeNameMap = [];
        foreach (ICardSet set in Sets)
            cardSetToTypeNameMap.Add(set.TypeName, set);

        foreach (ICard card in cards) {
            if (cardSetToTypeNameMap.TryGetValue(card.ParentTypeName, out ICardSet? parentSet)) {
                if (parentSet?.MemberTypeName != card.TypeName) {
                    _logger?.LogWarning("{Card} was registered as a member of {Set} but member and parent type names were mismatched. ", card, parentSet);
                    continue;
                }
                parentSet.Cards ??= [];
                if (parentSet.Cards.Contains(card))
                    continue;
                card.CardSet = parentSet;
                parentSet.Cards.Add(card);
                continue;
            }
            if (registry[card.ParentTypeName] is not Type parentType) {
                _logger?.LogWarning("The name {Name} registered as parent type for {Card} was not found in the registry.", card.ParentTypeName, card);
                continue;
            }
            if (parentType.Name != card.ParentTypeName) {
                _logger?.LogWarning("{Name}, the name of the type registered as parent of {Card}, did not match the expected value ({Expected}).", parentType.Name, card, card.ParentTypeName);
                continue;
            }
            var setObject = Activator.CreateInstance(parentType);
            if (setObject is not ICardSet parentSetObject) {
                _logger?.LogWarning("Activation of type {Type}, which was registered as parent of {Card}, failed.", parentType, card);
                continue;
            }
            if (parentSetObject.MemberTypeName != card.TypeName) {
                _logger?.LogWarning("{Name}, the name of the type registered as member of {Set}, did not match the expected value({Expected}).", card.TypeName, card, card.ParentTypeName);
                continue;
            }
            card.CardSet = parentSetObject;
            parentSetObject.Cards.Add(card);
            cardSetToTypeNameMap.Add(parentType.Name, parentSetObject);
        }
        if (Sets.Count < cardSetToTypeNameMap.Values.Count)
            foreach (ICardSet loadedSet in cardSetToTypeNameMap.Values)
                if (!Sets.Contains(loadedSet))
                    Sets.Add(loadedSet);
    }
    /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>
    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(async () =>
        {
            List<SerializedData> serials = [];
            int numLibrary = GameDeck.Library.Count;
            serials.Add(new(typeof(int), [numLibrary]));
            for (int i = 0; i < numLibrary; i++) {
                ICard currentCard = GameDeck.Library[i];
                IEnumerable<SerializedData> cardSerials = await currentCard.GetBinarySerials();
                serials.AddRange(cardSerials ?? []);
            }
            int numDiscard = GameDeck.DiscardPile.Count;
            serials.Add(new(typeof(int), [numDiscard]));
            for (int i = 0; i < numDiscard; i++) {
                ICard currentCard = GameDeck.DiscardPile[i];
                IEnumerable<SerializedData> cardSerials = await currentCard.GetBinarySerials();
                serials.AddRange(cardSerials ?? []);
            }

            return serials.ToArray();
        });
    }
    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
    public bool LoadFromBinary(BinaryReader reader)
    {
        GameDeck.Library = [];
        GameDeck.DiscardPile = [];
        bool loadComplete = true;

        try {
            int numLibrary = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            List<ICard> newLibrary = [];
            for (int i = 0; i < numLibrary; i++) {
                string typeName = reader.ReadString();
                if (CardFactory.BuildCard(typeName) is not ICard newCard) {
                    _logger?.LogWarning("{CardFactory} failed to construct a card of type {name} during loading of {base}.", CardFactory, typeName, this);
                    loadComplete = false;
                    continue;
                }
                newCard.Logger = _loggerFactory.CreateLogger<TroopCard>();
                newCard.LoadFromBinary(reader);
                newLibrary.Add(newCard);
            }
            List<ICard> newDiscard = [];
            int numDiscard = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            for (int i = 0; i < numDiscard; i++) {
                string typeName = reader.ReadString();
                if (CardFactory.BuildCard(typeName) is not ICard newCard) {
                    _logger?.LogWarning("{CardFactory} failed to construct a card of type {name} during loading of {base}.", CardFactory, typeName, this);
                    loadComplete = false;
                    continue;
                }
                newCard.LoadFromBinary(reader);
                newDiscard.Add(newCard);
            }

            InitializeLibrary([.. newLibrary]);
            InitializeDiscardPile([.. newDiscard]);
        } catch (Exception ex) {
            _logger?.LogError("An exception was thrown while loading {CardBase}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
}
