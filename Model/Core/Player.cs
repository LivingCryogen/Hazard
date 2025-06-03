using Microsoft.Extensions.Logging;
using Model.Entities.Cards;
using Model.EventArgs;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;

namespace Model.Core;
/// <inheritdoc cref="IPlayer"/>.
public class Player : IPlayer
{
    private readonly ILogger _logger;
    private readonly IRuleValues _values;
    private readonly IBoard _board;
    private readonly CardFactory _cardFactory;
    private int _armyPool;
    /// <summary>
    /// Constructs a player when their name is unknown.
    /// </summary>
    /// <param name="number">The number of the player (0 or higher).</param>
    /// <param name="numPlayers">The number of total players in the game.</param>
    /// <param name="values">Provides game-rule defined values and equations.</param>
    /// <param name="board">The game board.</param>
    /// <param name="logger">A logger for logging errors and debug information.</param>  
    /// <param name="cardFactory">A factory for producing <see cref="ICard"/>s; necessary for populating <see cref="Hand"/> via <see cref="LoadFromBinary(BinaryReader)"/>.</param>
    public Player(int number, int numPlayers, CardFactory cardFactory, IRuleValues values, IBoard board, ILogger<Player> logger)
    {
        _logger = logger;
        Name = string.Empty;
        Number = number;
        ControlledTerritories = [];
        _values = values;
        ArmyPool = _values.SetupStartingPool[numPlayers];
        Hand = [];
        _board = board;
        _cardFactory = cardFactory;
    }
    /// <summary>
    /// Constructs a player with a user-provided name.
    /// </summary>
    /// <param name="name">The name for the player provided by the user.</param>
    /// <param name="number">The number of the player (0 or higher).</param>
    /// <param name="numPlayers">The number of total players in the game.</param>
    /// <param name="values">Provides game-rule defined values and equations.</param>
    /// <param name="board">The game board.</param>
    /// <param name="logger">A logger for logging errors and debug information.</param>  
    /// <param name="cardFactory">A factory for producing <see cref="ICard"/>s; necessary for populating <see cref="Hand"/> via <see cref="LoadFromBinary(BinaryReader)"/>.</param>
    public Player(string name, int number, int numPlayers, CardFactory cardFactory, IRuleValues values, IBoard board, ILogger<Player> logger)
    {
        _logger = logger;
        Name = name;
        Number = number;
        ControlledTerritories = [];
        _values = values;
        ArmyPool = _values.SetupStartingPool![numPlayers];
        Hand = [];
        _board = board;
        _cardFactory = cardFactory;
    }

    /// <inheritdoc cref="IPlayer.PlayerChanged"/>.
    public event EventHandler<IPlayerChangedEventArgs>? PlayerChanged;
    /// <inheritdoc cref="IPlayer.PlayerLost"/>.
    public event EventHandler? PlayerLost;
    /* <inheritdoc cref="IPlayer.PlayerWon"/>.
    /// <remarks>
    /// Unnecessary in the base game, but likely to be used when implementing Secret Missions.
    /// </remarks>
     public event EventHandler? PlayerWon;*/

    /// <inheritdoc cref="IPlayer.Name"/>.
    public string Name { get; set; }
    /// <inheritdoc cref="IPlayer.Number"/>.
    public int Number { get; private set; }
    /// <inheritdoc cref="IPlayer.HasCardSet"/>.
    public bool HasCardSet { get; set; } = false;
    /// <inheritdoc cref="IPlayer.ArmyBonus"/>.
    public int ArmyBonus => CalculateTotalBonus();
    /// <inheritdoc cref="IPlayer.ContinentBonus"/>.
    public int ContinentBonus { get; set; }
    /// <inheritdoc cref="IPlayer.ArmyPool"/>.
    public int ArmyPool
    {
        get { return _armyPool; }
        set
        {
            bool changed = !_armyPool.Equals(value);
            if (changed)
            {
                _armyPool = value;
                PlayerChanged?.Invoke(this, new PlayerChangedEventArgs(nameof(ArmyPool)));
            }
        }
    }
    /// <inheritdoc cref="IPlayer.ControlledTerritories"/>.
    public HashSet<TerrID> ControlledTerritories { get; private set; } = [];
    /// <inheritdoc cref="IPlayer.Hand"/>.
    public List<ICard> Hand { get; set; } = [];

    private int CalculateTotalBonus()
    {
        return _values.CalculateArmyBonus(ControlledTerritories.Count, _board[Number, nameof(ContID)].Cast<ContID>().ToList());
    }
    /// <inheritdoc cref="IPlayer.GetsTradeBonus(int)"/>.
    public void GetsTradeBonus(int tradeInBonus)
    {
        ArmyPool += tradeInBonus;
        PlayerChanged?.Invoke(this, new PlayerChangedEventArgs(nameof(ArmyPool)));
    }
    /// <inheritdoc cref="IPlayer.GetControlledTargets(TerrID[])"/>.
    public TerrID[] GetControlledTargets(TerrID[] targets)
    {
        return [.. targets.Intersect(ControlledTerritories)];
    }
    /// <inheritdoc cref="IPlayer.FindCardSet"/>
    public void FindCardSet()
    {
        List<ICardSet> setsInHand = [];
        List<ICard> tradeableCards = [];
        foreach (var card in Hand)
        {
            if (card.CardSet != null && !setsInHand.Contains(card.CardSet))
                setsInHand.Add(card.CardSet);

            if (card.IsTradeable == true)
                tradeableCards.Add(card);
        }

        if (setsInHand.Count == 0 || tradeableCards.Count == 0)
        {
            HasCardSet = false;
            return;
        }

        ICard[] tradeable = [.. tradeableCards];

        foreach (ICardSet cardSet in setsInHand)
        {
            var matches = cardSet.FindTradeSets(tradeable);
            if (matches == null || matches.Length == 0)
                HasCardSet = false;
            else if (matches.Length > 0)
                HasCardSet = true;
        }
    }
    /// <inheritdoc cref="IPlayer.AddTerritory(TerrID)"/>
    public bool AddTerritory(TerrID territory)
    {
        if (ControlledTerritories.Contains(territory))
            return false;

        ControlledTerritories.Add(territory);
        PlayerChanged?.Invoke(this, new PlayerChangedEventArgs(nameof(ControlledTerritories), null, territory));
        return true;
    }
    /// <inheritdoc cref="IPlayer.RemoveTerritory(TerrID)"/>
    public bool RemoveTerritory(TerrID territory)
    {
        if (!ControlledTerritories.Contains(territory))
            return false;

        ControlledTerritories.Remove(territory);
        PlayerChanged?.Invoke(this, new PlayerChangedEventArgs(nameof(ControlledTerritories), territory, null));
        if (ControlledTerritories.Count <= 0)
            PlayerLost?.Invoke(this, new());
        return true;
    }
    /// <inheritdoc cref="IPlayer.AddCard(ICard)"/>
    public void AddCard(ICard card)
    {
        Hand.Add(card);
        FindCardSet();
        PlayerChanged?.Invoke(this, new PlayerChangedEventArgs(nameof(Hand), null, card, Hand.Count - 1));
    }
    /// <inheritdoc cref="IPlayer.RemoveCard(int)"/>
    public bool RemoveCard(int handIndex)
    {
        if (handIndex >= Hand.Count)
            return false;

        var tempCard = Hand[handIndex];
        Hand.RemoveAt(handIndex);
        FindCardSet();
        PlayerChanged?.Invoke(this, new PlayerChangedEventArgs(nameof(Hand), tempCard, null, handIndex));
        return true;
    }
    /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>
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
            foreach (TerrID territory in ControlledTerritories)
                data.Add(new(typeof(TerrID), [territory]));
            data.Add(new(typeof(int), [Hand.Count]));
            for (int i = 0; i < Hand.Count; i++)
            {
                IEnumerable<SerializedData> cardSerials = await Hand[i].GetBinarySerials();
                data.AddRange(cardSerials ?? []);
            }
            return data.ToArray();
        });
    }
    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try
        {
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
            for (int i = 0; i < numCards; i++)
            {
                string cardTypeName = reader.ReadString();
                ICard newCard = _cardFactory.BuildCard(cardTypeName);
                newCard.LoadFromBinary(reader);
                Hand.Add(newCard);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("An exception was thrown while loading {Player}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }

        return loadComplete;
    }
}

