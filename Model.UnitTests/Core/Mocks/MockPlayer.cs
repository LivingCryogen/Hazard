using Microsoft.Extensions.Logging;
using Model.Tests.Entities.Mocks;
using Share.Enums;
using Share.Interfaces.Model;
using Share.Services.Serializer;

namespace Model.Tests.Core.Mocks;

internal class MockPlayer : IPlayer
{
    private readonly ILogger _logger;
    private readonly MockCardFactory _cardFactory;
    private readonly IBoard _board;

    public MockPlayer(int number, MockCardFactory cardFactory, IRuleValues values, IBoard board, ILogger<MockPlayer> logger)
    {
        _logger = logger;
        Number = number;
        ControlledTerritories = [];
        Hand = [];
        _board = board;
        _cardFactory = cardFactory;
    }

    public MockPlayer(string name, int number, MockCardFactory cardFactory, IRuleValues values, IBoard board, ILogger<MockPlayer> logger)
    {
        _logger = logger;
        Name = name;
        Number = number;
        ControlledTerritories = [];


        Hand = [];
        _board = board;
        _cardFactory = cardFactory;
    }

    public int ArmyBonus { get; }
    public int ArmyPool { get; set; } = 10;
    public int ContinentBonus { get; set; } = 6;
    public List<TerrID> ControlledTerritories { get; set; }
    public List<ICard> Hand { get; set; } = [];
    public string Name { get; set; } = "YourFatherSmeltOfElderBerries!";
    public int Number { get; set; }
    public bool HasCardSet { get; set; } = false;

#pragma warning disable CS0414 // For unit-testing, these are unused. If integration tests are built, they should be, at which time these warnings should be re-enabled.
    public event EventHandler<IPlayerChangedEventArgs>? PlayerChanged = null;
    public event EventHandler? PlayerLost = null;
    public event EventHandler? PlayerWon = null;
#pragma warning restore CS0414
    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(async () =>
        {
            List<SerializedData> data = [
                new (typeof(string), [Name]),
                new (typeof(int), [ArmyPool]),
                new (typeof(int), [ContinentBonus]),
                new (typeof(bool), [HasCardSet]),
                new (typeof(int), [ControlledTerritories.Count])
            ];
            for (int i = 0; i < ControlledTerritories.Count; i++)
                data.Add(new(typeof(TerrID), [ControlledTerritories[i]]));
            data.Add(new(typeof(int), [Hand.Count]));
            for (int i = 0; i < Hand.Count; i++) {
                IEnumerable<SerializedData> cardSerials = await Hand[i].GetBinarySerials();
                data.AddRange(cardSerials ?? []);
            }
            return data.ToArray();
        });
    }

    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try {
            Name = (string)BinarySerializer.ReadConvertible(reader, typeof(string));
            ArmyPool = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            ContinentBonus = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            HasCardSet = (bool)BinarySerializer.ReadConvertible(reader, typeof(bool));
            int numControlledTerritories = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            ControlledTerritories = [];
            for (int i = 0; i < numControlledTerritories; i++)
                ControlledTerritories.Add((TerrID)BinarySerializer.ReadConvertible(reader, typeof(TerrID)));
            int numCards = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            Hand = [];
            for (int i = 0; i < numCards; i++) {
                string cardTypeName = reader.ReadString();
                ICard newCard = _cardFactory.BuildCard(cardTypeName);
                newCard.LoadFromBinary(reader);
                Hand.Add(newCard);
            }
        } catch (Exception ex) {
            _logger.LogError("An exception was thrown while loading {Player}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }

        return loadComplete;
    }

    public bool AddCard(ICard card)
    {
        throw new NotImplementedException();
    }
    public bool AddTerritory(TerrID territory)
    {
        throw new NotImplementedException();
    }
    public TerrID[] GetControlledTargets(TerrID[] targets)
    {
        throw new NotImplementedException();
    }
    public void GetsTradeBonus(int tradeInBonus)
    {
        throw new NotImplementedException();
    }
    public bool RemoveCard(int handIndex)
    {
        throw new NotImplementedException();
    }
    public bool RemoveTerritory(TerrID territory)
    {
        throw new NotImplementedException();
    }
    public void FindCardSet()
    {
        List<ICardSet> setsInHand = [];
        List<ICard> tradeableCards = [];
        foreach (var card in Hand) {
            if (card.CardSet != null && !setsInHand.Contains(card.CardSet))
                setsInHand.Add(card.CardSet);

            if (card.IsTradeable == true)
                tradeableCards.Add(card);
        }

        if (setsInHand.Count == 0 || tradeableCards.Count == 0)
            HasCardSet = false;

        ICard[] tradeable = [.. tradeableCards];

        foreach (ICardSet cardSet in setsInHand) {
            if ((cardSet?.FindTradeSets(tradeable) ?? []).Length != 0)
                HasCardSet = true;
        }
    }
}
