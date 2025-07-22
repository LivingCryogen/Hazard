using Microsoft.Extensions.Logging;
using Model.Entities.Cards;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Registry;
using Shared.Services.Serializer;

namespace Model.Entities;

/// <inheritdoc cref="ICardBase{TerrID}"/>
/// <param name="loggerFactory">Instantiates loggers for logging debug information and errors (provided by DI).</param>
/// <param name="registry">The application's type registry.</param>
public class CardBase(ILoggerFactory loggerFactory, ITypeRegister<ITypeRelations> registry) : ICardBase<TerrID>, IBinarySerializable
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<CardBase>();
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    /// <summary>
    /// Gets a factory for making <see cref="ICard{T}"/>s.
    /// </summary>
    /// <remarks>
    /// Used when loading from a save file; see <see cref="LoadFromBinary"/>.
    /// </remarks>
    public ICardFactory<TerrID> CardFactory { get; } = new CardFactory(registry, loggerFactory);
    /// <summary>
    /// Gets or sets a list of card sets.
    /// </summary>
    public List<ICardSet<TerrID>> Sets { get; set; } = [];
    /// <summary>
    /// Gets or sets the deck of cards to be used for this game.
    /// </summary>
    public IDeck<TerrID> GameDeck { get; set; } = new Deck();
    /// <inheritdoc cref="ICardBase{T}.InitializeFromAssets(IAssetFetcher{T}, bool)"/>
    public void InitializeFromAssets(IAssetFetcher<TerrID> assetFetcher, bool defaultMode)
    {
        Sets = assetFetcher.FetchCardSets();
        if (Sets.Count == 0)
            return;

        List<ICard<TerrID>> defaultCards = [];
        foreach (var set in Sets)
        {
            if (set.Cards.Count == 0)
                continue;
            if (set.Cards.OfType<ITroopCard<TerrID>>().Count() == set.Cards.Count)
            {
                defaultCards.AddRange(set.Cards);
                var setTypeName = set.GetType().Name;
                foreach (ICard<TerrID> card in defaultCards)
                    if (card.CardSet == null && set.IsParent(card))
                        card.CardSet = set;
            }
        }
        if (defaultMode)
            GameDeck = new Deck(defaultCards.ToArray());
        else
            GameDeck = new Deck(Sets.ToArray());

        GameDeck.Shuffle();
    }
    /// <inheritdoc cref="ICardBase{T}.InitializeLibrary(ICard{T}[])"/>
    public void InitializeLibrary(ICard<TerrID>[] cards)
    {
        MapCardsToSets(cards);
        GameDeck.Library.AddRange(cards);
    }
    /// <inheritdoc cref="ICardBase{T}.InitializeDiscardPile(ICard{T}[])"/>/>
    public void InitializeDiscardPile(ICard<TerrID>[] cards)
    {
        MapCardsToSets(cards);
        GameDeck.DiscardPile.AddRange(cards);
    }
    /// <inheritdoc cref="ICardBase{T}.MapCardsToSets(ICard{T}[])"/>
    public void MapCardsToSets(ICard<TerrID>[] cards)
    {
        Sets ??= [];

        Dictionary<string, ICardSet<TerrID>> cardSetToTypeNameMap = [];
        foreach (ICardSet<TerrID> set in Sets)
            cardSetToTypeNameMap.Add(set.TypeName, set);

        foreach (ICard<TerrID> card in cards)
        {
            if (cardSetToTypeNameMap.TryGetValue(card.ParentTypeName, out ICardSet<TerrID>? parentSet))
            {
                if (parentSet?.MemberTypeName != card.TypeName)
                {
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
            if (registry[card.ParentTypeName] is not Type parentType)
            {
                _logger?.LogWarning("The name {Name} registered as parent type for {Card} was not found in the registry.", card.ParentTypeName, card);
                continue;
            }
            if (parentType.Name != card.ParentTypeName)
            {
                _logger?.LogWarning("{Name}, the name of the type registered as parent of {Card}, did not match the expected value ({Expected}).", parentType.Name, card, card.ParentTypeName);
                continue;
            }
            var setObject = Activator.CreateInstance(parentType);
            if (setObject is not ICardSet<TerrID> parentSetObject)
            {
                _logger?.LogWarning("Activation of type {Type}, which was registered as parent of {Card}, failed.", parentType, card);
                continue;
            }
            if (parentSetObject.MemberTypeName != card.TypeName)
            {
                _logger?.LogWarning("{Name}, the name of the type registered as member of {Set}, did not match the expected value({Expected}).", card.TypeName, card, card.ParentTypeName);
                continue;
            }
            card.CardSet = parentSetObject;
            parentSetObject.Cards.Add(card);
            cardSetToTypeNameMap.Add(parentType.Name, parentSetObject);
        }
        if (Sets.Count < cardSetToTypeNameMap.Values.Count)
            foreach (ICardSet<TerrID> loadedSet in cardSetToTypeNameMap.Values)
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
            for (int i = 0; i < numLibrary; i++)
            {
                ICard<TerrID> currentCard = GameDeck.Library[i];
                IEnumerable<SerializedData> cardSerials = await currentCard.GetBinarySerials();
                serials.AddRange(cardSerials ?? []);
            }
            int numDiscard = GameDeck.DiscardPile.Count;
            serials.Add(new(typeof(int), [numDiscard]));
            for (int i = 0; i < numDiscard; i++)
            {
                ICard<TerrID> currentCard = GameDeck.DiscardPile[i];
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

        try
        {
            int numLibrary = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            List<ICard<TerrID>> newLibrary = [];
            for (int i = 0; i < numLibrary; i++)
            {
                string typeName = reader.ReadString();
                if (CardFactory.BuildCard(typeName) is not ICard<TerrID> newCard)
                {
                    _logger?.LogWarning("{CardFactory} failed to construct a card of type {name} during loading of {base}.", CardFactory, typeName, this);
                    loadComplete = false;
                    continue;
                }
                newCard.Logger = _loggerFactory.CreateLogger<TroopCard>();
                newCard.LoadFromBinary(reader);
                newLibrary.Add(newCard);
            }
            List<ICard<TerrID>> newDiscard = [];
            int numDiscard = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            for (int i = 0; i < numDiscard; i++)
            {
                string typeName = reader.ReadString();
                if (CardFactory.BuildCard(typeName) is not ICard<TerrID> newCard)
                {
                    _logger?.LogWarning("{CardFactory} failed to construct a card of type {name} during loading of {base}.", CardFactory, typeName, this);
                    loadComplete = false;
                    continue;
                }
                newCard.LoadFromBinary(reader);
                newDiscard.Add(newCard);
            }

            InitializeLibrary([.. newLibrary]);
            InitializeDiscardPile([.. newDiscard]);
        }
        catch (Exception ex)
        {
            _logger?.LogError("An exception was thrown while loading {CardBase}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
}
