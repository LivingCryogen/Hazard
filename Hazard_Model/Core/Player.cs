using Hazard_Model.EventArgs;
using Hazard_Share.Enums;
using Hazard_Share.Interfaces.Model;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;

namespace Hazard_Model.Core;
/// <inheritdoc cref="IPlayer"/>.
public class Player : IPlayer
{
    private readonly ILogger _logger;
    private readonly IRuleValues _values;
    private readonly IBoard _board;
    private int _armyPool;
    /// <summary>
    /// Builds a <see cref="IPlayer"/> given certain rules values and an initial board state.
    /// </summary>
    /// <param name="name">The name of the player.</param>
    /// <param name="number">The number of the player (0 or higher).</param>
    /// <param name="numPlayers">The number of total players in the game.</param>
    /// <param name="values">The <see cref="IRuleValues"/> implementation providing game-rule defined values and equations.</param>
    /// <param name="board">The <see cref="IBoard"/> implementation describing initial board state.</param>
    /// <param name="logger">An <see cref="ILogger"/>. Note that, since the <see cref="DataAccess.BinarySerializer"/> is responsible for initializing this on <see cref="DataAccess.BinarySerializer.LoadGame"/>, <br/>
    /// this logger must be provided by another class object, and is *not* injected via DI.</param>
    public Player(string name, int number, int numPlayers, IRuleValues values, IBoard board, ILogger logger)
    {
        _logger = logger;
        Name = name;
        Number = number;
        ControlledTerritories = [];
        _values = values;
        ArmyPool = _values!.SetupStartingPool![numPlayers];
        Hand = [];
        _board = board;
    }

    /// <inheritdoc cref="IPlayer.PlayerChanged"/>.
    public event EventHandler<IPlayerChangedEventArgs>? PlayerChanged;
    /// <inheritdoc cref="IPlayer.PlayerLost"/>.
    public event EventHandler? PlayerLost;
    /// <inheritdoc cref="IPlayer.PlayerWon"/>.
    public event EventHandler? PlayerWon;

    #region Properties
    /// <inheritdoc cref="IPlayer.Name"/>.
    public string Name { get; init; }
    /// <inheritdoc cref="IPlayer.Number"/>.
    public int Number { get; init; }
    /// <inheritdoc cref="IPlayer.HasCardSet"/>.
    public bool HasCardSet { get; set; } = false;
    /// <inheritdoc cref="IPlayer.ArmyBonus"/>.
    public int ArmyBonus => CalculateTotalBonus();
    /// <inheritdoc cref="IPlayer.ContinentBonus"/>.
    public int ContinentBonus { get; set; }
    /// <inheritdoc cref="IPlayer.ArmyPool"/>.
    public int ArmyPool {
        get { return _armyPool; }
        set {
            bool changed = !_armyPool.Equals(value);
            if (changed) {
                _armyPool = value;
                PlayerChanged?.Invoke(this, new PlayerChangedEventArgs(nameof(ArmyPool)));
            }
        }
    }
    /// <inheritdoc cref="IPlayer.Hand"/>.
    public List<ICard> Hand { get; set; }
    /// <inheritdoc cref="IPlayer.ControlledTerritories"/>.
    public List<TerrID> ControlledTerritories { get; set; }
    #endregion

    #region Methods

    public IEnumerable<IConvertible> GetSaveData()
    {
        List<object> data = [Name, _armyPool, ContinentBonus, ControlledTerritories.Count];
        foreach (TerrID territory in ControlledTerritories)
            data.Add((int)territory)
        data.Add((Hand.Count, typeof(int)));
        foreach (ICard card in Hand)
            data.Add((card.GetSaveData(_logger), typeof(ICard)));
        return data;


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
        List<TerrID> bonusTargets = [];
        foreach (TerrID target in targets)
            if (ControlledTerritories.Contains(target))
                bonusTargets.Add(target);

        return [.. bonusTargets]; // note : multiple possible targets must be winnowed down to 1 per rules; Input req'd
    }
    /// <summary>
    /// Fires when <see cref="IPlayer.ControlledTerritories"/> changes.
    /// </summary>
    /// <remarks>
    /// Alerts the ViewModel to internal changes of this <see cref="IPlayer"/>'s <see cref="ControlledTerritories"/>.
    /// </remarks>
    /// <param name="sender">The internal collection, <see cref="ControlledTerritories"/>.</param>
    /// <param name="e">An <see cref="NotifyCollectionChangedEventArgs"/> instance storing information about the changed member of <see cref="ControlledTerritories"/>.</param>
    public void OnControlledTerritoriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e != null) {
            if (!(e.OldItems == null && e.NewItems == null)) {
                PlayerChanged?.Invoke(this, new PlayerChangedEventArgs(nameof(ControlledTerritories), e.OldItems?[0], e.NewItems?[0]));

                if (e.NewItems != null) {
                    if (ControlledTerritories.Count >= _board.Geography.NumTerritories)
                        PlayerWon?.Invoke(this, new());
                }

                if (e.OldItems != null) {
                    if (ControlledTerritories.Count <= 0)
                        PlayerLost?.Invoke(this, new());
                }
            }
        }
    }
    /// <inheritdoc cref="IPlayer.FindCardSet"/>
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

        if (setsInHand.Count == 0 || tradeableCards.Count == 0) {
            HasCardSet = false;
            return;
        }

        ICard[] tradeable = [.. tradeableCards];

        foreach (ICardSet cardSet in setsInHand) {
            var matches = cardSet.FindTradeSets(tradeable);
            if (matches == null || matches.Length == 0)
                HasCardSet = false;
            else if (matches.Length > 0)
                HasCardSet = true;
        }
    }
    private int CalculateTotalBonus()
    {
        return _values.CalculateArmyBonus(ControlledTerritories.Count, _board[Number, nameof(ContID)].Cast<ContID>().ToList());
    }
    /// <inheritdoc cref="IPlayer.AddTerritory(TerrID)"/>
    public bool AddTerritory(TerrID territory)
    {
        if (ControlledTerritories != null) {
            if (ControlledTerritories.Contains(territory) == false) {
                ControlledTerritories.Add(territory);
                PlayerChanged?.Invoke(this, new PlayerChangedEventArgs(nameof(ControlledTerritories), null, territory));
                if (ControlledTerritories.Count >= _board.Geography.NumTerritories)
                    PlayerWon?.Invoke(this, new());
                return true;
            }
            else return false;
        }
        else return false;
    }
    /// <inheritdoc cref="IPlayer.RemoveTerritory(TerrID)"/>
    public bool RemoveTerritory(TerrID territory)
    {
        if (ControlledTerritories != null) {
            if (ControlledTerritories.Contains(territory) == true) {
                ControlledTerritories.Remove(territory);
                PlayerChanged?.Invoke(this, new PlayerChangedEventArgs(nameof(ControlledTerritories), territory, null));
                if (ControlledTerritories.Count <= 0)
                    PlayerLost?.Invoke(this, new());
                return true;
            }
            else return false;
        }
        else return false;
    }
    /// <inheritdoc cref="IPlayer.AddCard(ICard)"/>
    public bool AddCard(ICard card)
    {
        if (Hand != null) {
            Hand.Add(card);
            FindCardSet();
            PlayerChanged?.Invoke(this, new PlayerChangedEventArgs(nameof(Hand), null, card, Hand.Count - 1));
            if (Hand.Contains(card))
                return true;
            else return false;
        }
        else return false;
    }
    /// <inheritdoc cref="IPlayer.RemoveCard(int)"/>
    public bool RemoveCard(int handIndex)
    {
        if (Hand != null && handIndex < Hand.Count) {
            var temp = Hand[handIndex];
            Hand.RemoveAt(handIndex);
            FindCardSet();
            PlayerChanged?.Invoke(this, new PlayerChangedEventArgs(nameof(Hand), temp, null, handIndex));
            return true;
        }
        else return false;
    }
    #endregion
}

