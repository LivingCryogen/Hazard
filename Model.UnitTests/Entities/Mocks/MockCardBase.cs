using Microsoft.Extensions.Logging;
using Microsoft.Testing.Platform.Logging;
using Model.Entities;
using Model.Entities.Cards;
using Model.Tests.DataAccess.Mocks;
using Model.Tests.Fixtures;
using Model.Tests.Fixtures.Mocks;
using Model.Tests.Fixtures.Stubs;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Registry;
using Shared.Services.Serializer;

namespace Model.Tests.Entities.Mocks;

public class MockCardBase : ICardBase
{
    private readonly Microsoft.Extensions.Logging.ILogger _logger = new LoggerStubT<MockCardBase>();
    private readonly MockCardSetData mockData = new();

    public MockCardBase(ITypeRegister<ITypeRelations> registry)
    {
        Sets = [];
        mockData.BuildFromMockData();
        MockCardSet mockSet = new() { JData = mockData };
        int numMockCards = mockData.Targets.Length;
        if (numMockCards != mockData.Insignia.Length)
            throw new Exception($"{mockData} returned improper data.");
        List<MockCard> mockCards = [];
        for (int i = 0; i < numMockCards; i++)
        {
            MockCard newMock = new(mockSet) { Target = mockData.Targets[i], Insigne = mockData.Insignia[i] };
            newMock.FillTestValues();
            mockCards.Add(newMock);
        }
        mockSet.Cards = [.. mockCards];
        Sets.Add(mockSet);
        GameDeck.Library.AddRange(mockCards);
        CardFactory = new MockCardFactory(mockSet);
    }

    public ICardFactory CardFactory { get; set; }
    public IDeck GameDeck { get; set; } = new Deck();
    public List<ICardSet> Sets { get; set; } = [];

    public void InitializeDiscardPile(ICard[] cards)
    {
        MapCardsToSets(cards);
        GameDeck.DiscardPile.AddRange(cards);
    }

    public void InitializeFromAssets(IAssetFetcher assetFetcher, bool defaultMode)
    {
        throw new NotImplementedException();
    }

    public void InitializeLibrary(ICard[] cards)
    {
        MapCardsToSets(cards);
        GameDeck.Library.AddRange(cards);
    }

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
            if (SharedRegister.Registry[card.ParentTypeName] is not Type parentType)
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

    public void Wipe()
    {
        Sets.Clear();
        GameDeck.Library.Clear();
        GameDeck.DiscardPile.Clear();
    }

    void ICardBase.InitializeFromAssets(IAssetFetcher assetFetcher, bool defaultMode)
    {
        throw new NotImplementedException();
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
                IEnumerable<SerializedData> cardSerials = await currentCard.GetBinarySerials();
                serials.AddRange(cardSerials ?? []);
            }
            int numDiscard = GameDeck.DiscardPile.Count;
            serials.Add(new(typeof(int), [numDiscard]));
            for (int i = 0; i < numDiscard; i++)
            {
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

        try
        {
            int numLibrary = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            List<ICard> newLibrary = [];
            for (int i = 0; i < numLibrary; i++)
            {
                string typeName = reader.ReadString();
                if (CardFactory.BuildCard(typeName) is not ICard newCard)
                {
                    _logger?.LogWarning("{CardFactory} failed to construct a card of type {name} during loading of {base}.", CardFactory, typeName, this);
                    loadComplete = false;
                    continue;
                }
                newCard.Logger = LoggerFactoryStub.CreateLogger<TroopCard>();
                newCard.LoadFromBinary(reader);
                newLibrary.Add(newCard);
            }
            List<ICard> newDiscard = [];
            int numDiscard = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            for (int i = 0; i < numDiscard; i++)
            {
                string typeName = reader.ReadString();
                if (CardFactory.BuildCard(typeName) is not ICard newCard)
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
