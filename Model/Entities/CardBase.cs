using Microsoft.Extensions.Logging;
using Model.Entities.Cards;
using Shared.Interfaces.Model;
using Shared.Services.Registry;
using Shared.Services.Serializer;

namespace Model.Entities;

/// <inheritdoc cref="ICardBase"/>
/// <param name="loggerFactory">Instantiates loggers for logging debug information and errors (provided by DI).</param>
/// <param name="registry">The application's type registry.</param>
public class CardBase(ILoggerFactory loggerFactory, ITypeRegister<ITypeRelations> registry) : ICardBase, IBinarySerializable
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<CardBase>();
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    /// <summary>
    /// Gets a factory for making <see cref="ICard"/>s.
    /// </summary>
    /// <remarks>
    /// Used when loading from a save file; see <see cref="LoadFromBinary"/>.
    /// </remarks>
    public ICardFactory CardFactory { get; } = new CardFactory(registry, loggerFactory);
    /// <summary>
    /// Gets or sets a list of card sets.
    /// </summary>
    public List<ICardSet> Sets { get; set; } = [];
    /// <summary>
    /// Gets or sets the deck of cards to be used for this game.
    /// </summary>
    public IDeck GameDeck { get; set; } = new Deck();
    /// <inheritdoc cref="ICardBase.Reward"/>
    public ICard? Reward { get; set; } = null;
    /// <inheritdoc cref="ICardBase.InitializeFromAssets(IAssetFetcher, bool)"/>
    public void InitializeFromAssets(IAssetFetcher assetFetcher, bool defaultMode)
    {
        Sets = assetFetcher.FetchCardSets();
        if (Sets.Count == 0)
            return;

        List<ICard> defaultCards = [];
        foreach (var set in Sets)
        {
            if (set.Cards.Count == 0)
                continue;
            if (set.Cards.OfType<ITroopCard>().Count() == set.Cards.Count)
            {
                defaultCards.AddRange(set.Cards);
                var setTypeName = set.GetType().Name;
                foreach (ICard card in defaultCards)
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
    /// <inheritdoc cref="ICardBase.InitializeLibrary(ICard[])"/>
    public void InitializeLibrary(ICard[] cards)
    {
        MapCardsToSets(cards);
        GameDeck.Library.AddRange(cards);
    }
    /// <inheritdoc cref="ICardBase.InitializeDiscardPile(ICard[])"/>/>
    public void InitializeDiscardPile(ICard[] cards)
    {
        MapCardsToSets(cards);
        GameDeck.DiscardPile.AddRange(cards);
    }
    /// <inheritdoc cref="ICardBase.SetReward"/>
    public bool SetReward()
    {
        try
        {
            Reward = GameDeck.DrawCard();
        }
        catch (InvalidOperationException ex)
        {
            _logger?.LogError("Failed to set reward: {Message}", ex.Message);
            return false;
        }

        if (Reward == null)
        {
            _logger?.LogError("Cardbase failed to set reward on request.");
            return false;
        }

        return true;
    }
    /// <inheritdoc cref="ICardBase.FetchReward"/>
    public ICard? FetchReward()
    {
        ICard? rewardCard = Reward;
        Reward = null;
        return rewardCard;
    }
    /// <inheritdoc cref="ICardBase.MapCardsToSets(ICard[])"/>
    public void MapCardsToSets(ICard[] cards)
    {
        Sets ??= [];

        Dictionary<string, ICardSet> cardSetToTypeNameMap = [];
        foreach (ICardSet set in Sets)
            cardSetToTypeNameMap.Add(set.TypeName, set);

        foreach (ICard card in cards)
        {
            if (cardSetToTypeNameMap.TryGetValue(card.ParentTypeName, out ICardSet? parentSet))
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
            if (setObject is not ICardSet parentSetObject)
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
            for (int i = 0; i < numLibrary; i++)
            {
                ICard currentCard = GameDeck.Library[i];
                serials.AddRange(await currentCard.GetBinarySerials());
            }
            int numDiscard = GameDeck.DiscardPile.Count;
            serials.Add(new(typeof(int), [numDiscard]));
            for (int i = 0; i < numDiscard; i++)
            {
                ICard currentCard = GameDeck.DiscardPile[i];
                serials.AddRange(await currentCard.GetBinarySerials());
            }
            if (Reward != null)
            {
                serials.Add(new(typeof(int), 1));
                serials.AddRange(await Reward.GetBinarySerials());
            }
            else
                serials.Add(new(typeof(int), 0));
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
            List<ICard> newLibrary = [];
            for (int i = 0; i < numLibrary; i++)
            {
                if (LoadCard(reader) is not ICard loadedCard)
                    continue;
                else
                    newLibrary.Add(loadedCard);
            }
            List<ICard> newDiscard = [];
            int numDiscard = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            for (int i = 0; i < numDiscard; i++)
            {
                if (LoadCard(reader) is not ICard loadedCard)
                    continue;
                else
                    newDiscard.Add(loadedCard);
            }
            bool hasReward = (int)BinarySerializer.ReadConvertible(reader, typeof(int)) == 1;
            if (hasReward)
            {
                Reward = LoadCard(reader);
            }
            InitializeLibrary([.. newLibrary]);
            InitializeDiscardPile([.. newDiscard]);
            if (Reward == null && hasReward)
                _logger.LogWarning("Reward card should be present but failed to load.");
            else if (Reward != null)
                MapCardsToSets([Reward]);
        }
        catch (Exception ex)
        {
            _logger?.LogError("An exception was thrown while loading {CardBase}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }

    private ICard? LoadCard(BinaryReader reader)
    {
        string typeName = reader.ReadString();
        if (CardFactory.BuildCard(typeName) is not ICard newCard)
        {
            _logger?.LogWarning("{CardFactory} failed to construct a card of type {name} during loading of {base}.", CardFactory, typeName, this);
            return null;
        }
        newCard.Logger = _loggerFactory.CreateLogger<TroopCard>();
        newCard.LoadFromBinary(reader);
        return newCard;
    }
}
