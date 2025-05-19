using Microsoft.Extensions.Logging;
using Model.Core;
using Model.EventArgs;
using Shared.Enums;
using Shared.Geography;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;
using System.Reflection.PortableExecutable;

namespace Model.Core;

/// <inheritdoc cref="IRegulator"/>
public class Regulator(ILogger<Regulator> logger, IGame currentGame) : IRegulator
{
    private readonly IGame _currentGame = currentGame;
    private readonly StateMachine _machine = currentGame.State;
    private readonly ILogger _logger = logger;
    private readonly int _numPlayers = currentGame.State.NumPlayers;
    private int _actionsCounter = 0;
    private int _prevActionCount = 0;

    // State Check Properties
    private int PlayerTurn => _machine.PlayerTurn;
    private GamePhase CurrentPhase => _machine.CurrentPhase;
    private bool InSetupPhase => _machine.CurrentPhase == GamePhase.DefaultSetup || _machine.CurrentPhase == GamePhase.TwoPlayerSetup;

    /// <inheritdoc cref="IRegulator.CurrentActionsLimit"/>
    public int CurrentActionsLimit { get; set; }
    /// <inheritdoc cref="IRegulator.PhaseActions"/>
    public int PhaseActions => _actionsCounter - _prevActionCount;
    /// <inheritdoc cref="IRegulator.Reward"/>
    public ICard? Reward { get; set; } = null;

    /// <inheritdoc cref="IRegulator.PromptBonusChoice"/>
    public event EventHandler<TerrID[]>? PromptBonusChoice;
    /// <inheritdoc cref="IRegulator.PromptTradeIn"/>
    public event EventHandler<IPromptTradeEventArgs>? PromptTradeIn;

    private bool IsInSecondStage()
    {
        return _machine.PhaseStageTwo;
    }
    private void SetSecondStage(bool newState)
    {
        _machine.PhaseStageTwo = newState;
    }
    private bool ReachedSecondStage()
    {
        bool exceededTerritoryLimit = ActionsExceedTerritoryCount();
        return (CurrentPhase, IsInSecondStage(), exceededTerritoryLimit) switch {
            (GamePhase.DefaultSetup, false, true) => true,
            (GamePhase.Move, false, _) => true,
            _ => false
        };
    }
    private void HandleStateChanged(object? sender, string propName)
    {
        if (propName != "CurrentPhase")
            return;
        var phase = CurrentPhase;
        _prevActionCount = _actionsCounter;
        switch (phase) {
            case GamePhase.Place:
                // Update number of allowed player actions (based on armies available to place)
                CurrentActionsLimit = _actionsCounter;
                _currentGame.Players[PlayerTurn].ArmyPool += _currentGame.Players[PlayerTurn].ArmyBonus;
                CurrentActionsLimit += _currentGame.Players[PlayerTurn].ArmyPool;

                // Check for trade-in and whether it must be forced (cards in hand limit reached)
                if (!_currentGame.Players[PlayerTurn].HasCardSet)
                    break;
                bool force = false;
                if (_currentGame.Players[PlayerTurn].Hand.Count >= 5)
                    force = true;
                PromptTradeIn?.Invoke(this, new PromptTradeEventArgs(PlayerTurn, force));
                break;

            case GamePhase.Attack:
                _currentGame.State.PhaseStageTwo = false;
                break;

            case GamePhase.Move:
                CurrentActionsLimit = _actionsCounter + 1;
                break;
        }
    }
    private void IncrementAction()
    {
        _actionsCounter++;

        if (ReachedSecondStage())
            SetSecondStage(true);

        if (_actionsCounter >= CurrentActionsLimit)
            ActionLimitHit();
    }
    private bool ActionsExceedTerritoryCount()
    {
        return _actionsCounter >= BoardGeography.NumTerritories;
    }
    private void ActionLimitHit()
    {
        if (InSetupPhase)
            _machine.IncrementRound();
        else
            _machine.IncrementPhase();
    }
    private void ForceDiscard(Player player, int[] handIndices)
    {
        foreach (int discardIndex in handIndices.OrderByDescending(i => i)) { // If the indices remain in ascending order, .RemoveAt() will improperly affect the list in subsequent iterations
            _currentGame.Cards.GameDeck.Discard(player.Hand[discardIndex]);
            player.RemoveCard(discardIndex);
        }
    }
    private ICard[] GetCardsFromHand(int playerNum, int[] handIndices)
    {
        var player = _currentGame.Players[playerNum];
        List<ICard> selectedCards = [];
        if (handIndices.Length > player.Hand.Count)
            throw new IndexOutOfRangeException($"An attempt was made to get {handIndices.Length} cards from {player}'s hand, but they only had {player.Hand.Count}.");

        foreach (int index in handIndices) {
            var indCard = _currentGame.Players[playerNum].Hand[index];
            selectedCards.Add(indCard);
        }

        return [.. selectedCards];
    }
    /// <inheritdoc cref="IRegulator.Initialize"/>
    public void Initialize()
    {
        if (_actionsCounter == 0 && _currentGame.Values.SetupActionsPerPlayers.TryGetValue(_numPlayers, out int actions))
            CurrentActionsLimit = actions;

        if (CurrentPhase == GamePhase.TwoPlayerSetup) {
            _prevActionCount = _actionsCounter;
            if (_currentGame is Game game)
                game.TwoPlayerAutoSetup();
        }

        _machine.StateChanged += HandleStateChanged;
    }

    /// <inheritdoc cref="IRegulator.CanSelectTerritory(TerrID, TerrID)"/>
    public bool CanSelectTerritory(TerrID newSelected, TerrID oldSelected)
    {
        bool priorSelection = oldSelected switch {
            TerrID.Null => false,
            _ => true
        };

        int owner = _currentGame.Board.TerritoryOwner[newSelected];
        int territoryArmies = _currentGame.Board.Armies[newSelected];

        return CurrentPhase switch {
            GamePhase.DefaultSetup =>
                _machine.PhaseStageTwo switch {
                    false when owner == -1 => true, // claiming unowned territory
                    true when owner == _machine.PlayerTurn => true, // reinforcing owned territory
                    _ => false
                },

            GamePhase.TwoPlayerSetup =>
                _machine.PhaseStageTwo switch {
                    false when owner == _machine.PlayerTurn => true, // reinforcing auto-assigned territory
                    true when owner == -1 => true, // reinforcing AI territory
                    _ => false
                },

            GamePhase.Place => owner == _machine.PlayerTurn, // place an army on an owned territory

            // Attack and Move have two selection steps, so we differentiate based on whether there was a prior selection
            GamePhase.Attack when !priorSelection => owner == _machine.PlayerTurn && territoryArmies >= 2,

            GamePhase.Attack when priorSelection => owner != _machine.PlayerTurn && BoardGeography.GetNeighbors(oldSelected).Contains(newSelected),

            GamePhase.Move when !priorSelection => owner == _machine.PlayerTurn && territoryArmies >= 2,

            GamePhase.Move when priorSelection => 
                owner == _machine.PlayerTurn && 
                oldSelected != newSelected && 
                IsMoveDestination(owner, newSelected, oldSelected),
            _ => false
        };
    }
    private bool IsMoveDestination(int playerOwner, TerrID target, TerrID source)
    {
        var ownedNeighbors = BoardGeography.GetNeighbors(source)
            .Where(neighbor => _currentGame.Board.TerritoryOwner[neighbor] == playerOwner);
        if (!ownedNeighbors.Any())
            return false;
        if (ownedNeighbors.Contains(target))
            return true;
        return false;
    }
    public (TerrID Selection, bool RequestInput, int? MaxValue) SelectTerritory(TerrID selected, TerrID priorSelected)
    {
        var board = _currentGame.Board;
        if (board == null)
            return (TerrID.Null, false, null);

        bool havePriorSelection = priorSelected != TerrID.Null;
        TerrID postSelection = TerrID.Null;
        bool requestInput = false;
        int? maxValue = null;
        var phase = _machine.CurrentPhase;

        switch (phase) {
            case GamePhase.Attack when !IsInSecondStage():
                postSelection = selected;
                _machine.PhaseStageTwo = true;
                break;
            case GamePhase.Attack when IsInSecondStage():
                if (!havePriorSelection) {
                    _logger.LogError("A second selection was attempted during Attack phase, but no prior selection was provided.");
                    throw new ArgumentNullException(nameof(priorSelected));
                }
                postSelection = selected;
                requestInput = true;
                _machine.PhaseStageTwo = false;
                break;

            case GamePhase.Move when !IsInSecondStage():
                postSelection = selected;
                _machine.PhaseStageTwo = true;
                break;
            case GamePhase.Move when IsInSecondStage():
                if (!havePriorSelection) {
                    _logger.LogError("A second selection was attempted during Move phase, but no prior selection was provided.");
                    throw new ArgumentNullException(nameof(priorSelected));
                }
                postSelection = TerrID.Null;
                requestInput = true;
                maxValue = _currentGame.Board.Armies[priorSelected] - 1;
                _machine.PhaseStageTwo = false;
                break;
            default:
                ClaimOrReinforce(selected);
                postSelection = TerrID.Null;
            break;
        }

        return (postSelection, requestInput, maxValue);
    }
    /// <inheritdoc cref="IRegulator.ClaimOrReinforce(TerrID)"/>
    public void ClaimOrReinforce(TerrID territory)
    {
        switch (CurrentPhase) {
            case GamePhase.DefaultSetup:
                _currentGame.Players[PlayerTurn].ArmyPool--;

                if (!IsInSecondStage()) {
                    _currentGame.Board.Claims(PlayerTurn, territory);
                    _currentGame.Players[PlayerTurn].AddTerritory(territory);
                }
                else
                    _currentGame.Board.Reinforce(territory);

                IncrementAction();

                if (CurrentPhase == GamePhase.DefaultSetup)
                    _machine.IncrementPlayerTurn();

                break;
            case GamePhase.TwoPlayerSetup:
                _currentGame.Board.Reinforce(territory);
                IncrementAction();

                // Rules for 2-player setup (with passive NPC player) dictate each player places twice on their territory, once on NPC territory.
                // The following determines which step we're at by tracking the difference in action count (actDiff), which resets at 3.
                int actDiff = _actionsCounter - _prevActionCount;
                switch (actDiff) {
                    case 1:
                        _currentGame.Players[PlayerTurn].ArmyPool--;
                        break;
                    case 2:
                        _currentGame.Players[PlayerTurn].ArmyPool--;
                        SetSecondStage(true);
                        break;
                    case 3:
                        if (CurrentPhase == GamePhase.TwoPlayerSetup) {
                            SetSecondStage(false);
                            _machine.IncrementPlayerTurn();
                            _prevActionCount = _actionsCounter;
                        }
                        break;
                }
                break;
            case GamePhase.Place:
                _currentGame.Players[PlayerTurn].ArmyPool--;
                _currentGame.Board.Reinforce(territory);
                IncrementAction();
                break;
        }
    }
    
    /// <inheritdoc cref="IRegulator.MoveArmies(TerrID, TerrID, int)"/>
    public void MoveArmies(TerrID source, TerrID target, int armies)
    {
        _currentGame.Board.Reinforce(source, -armies);
        _currentGame.Board.Reinforce(target, armies);

        if (CurrentPhase == GamePhase.Move)
            IncrementAction();
    }
    /// <inheritdoc cref="IRegulator.Battle(TerrID, TerrID, ValueTuple{int, int}[])"/>
    public void Battle(TerrID source, TerrID target, (int AttackRoll, int DefenseRoll)[] diceRolls)
    {
        _actionsCounter++;

        int sourceLoss = 0;
        int targetLoss = 0;
        foreach (var (AttackRoll, DefenseRoll) in diceRolls)
            if (AttackRoll > DefenseRoll)
                targetLoss++;
            else
                sourceLoss++;

        if (targetLoss >= _currentGame.Board.Armies[target]) {
            int conqueredOwner = _currentGame.Board.TerritoryOwner[target];
            int newOwner = _currentGame.Board.TerritoryOwner[source];
            if (conqueredOwner > -1)
                _currentGame.Players[conqueredOwner].RemoveTerritory(target);
            _currentGame.Players[newOwner].AddTerritory(target);

            _currentGame.Board.Conquer(source, target, _currentGame.Board.TerritoryOwner[source]);

            Reward ??= _currentGame.Cards.GameDeck.DrawCard();
        }
        if (sourceLoss > 0)
            _currentGame.Board.Reinforce(source, -sourceLoss);
        if (targetLoss > 0)
            _currentGame.Board.Reinforce(target, -targetLoss);
    }
    /// <inheritdoc cref="IRegulator.CanTradeInCards(int, int[])"/>
    public bool CanTradeInCards(int playerNum, int[] handIndices)
    {
        if (playerNum != PlayerTurn)
            return false;
        if (handIndices.Length < 3)
            return false;
        var selectedCards = GetCardsFromHand(playerNum, handIndices);
        if (selectedCards == null || selectedCards.Length == 0)
            return false;

        List<ICardSet> tradedSets = [];
        foreach (var card in selectedCards) {
            if (card.IsTradeable == false)
                return false;

            if (card.CardSet == null) {
                _logger.LogError("{Card} did not have a CardSet specified on Trade-In check.", card);
                return false;
            }
            else if (!tradedSets.Contains(card.CardSet))
                tradedSets.Add(card.CardSet);
        }

        if (tradedSets.Count == 0)
            return false;

        foreach (var set in tradedSets) {
            if (!set.IsValidTrade([.. selectedCards]))
                return false;
        }

        return true;
    }
    /// <inheritdoc cref="IRegulator.TradeInCards(int, int[])"/>
    public void TradeInCards(int playerNum, int[] handIndices)
    {
        var selectedCards = GetCardsFromHand(playerNum, handIndices);
        if (selectedCards == null || selectedCards.Length == 0)
            return;

        var currentPlayer = _currentGame.Players[playerNum];
        _machine.IncrementNumTrades(1);
        int tradebonus = _currentGame.Values.CalculateBaseTradeInBonus(_machine.NumTrades);
        currentPlayer.GetsTradeBonus(tradebonus);
        CurrentActionsLimit += tradebonus;
        ForceDiscard((Player)currentPlayer, handIndices);

        var tradedTargets = selectedCards.SelectMany(item => item.Target);
        var controlledTargets = currentPlayer.GetControlledTargets(tradedTargets.ToArray());
        if (controlledTargets.Length == 1)
            _currentGame.Board.Reinforce(controlledTargets[0], _currentGame.Values.TerritoryTradeInBonus);
        else if (controlledTargets.Length > 1)
            PromptBonusChoice?.Invoke(this, [.. controlledTargets]);
    }
    /// <inheritdoc cref="IRegulator.AwardTradeInBonus(TerrID)"/>
    public void AwardTradeInBonus(TerrID territory)
    {
        _currentGame.Board.Reinforce(territory, _currentGame.Values.TerritoryTradeInBonus);
    }
    /// <inheritdoc cref="IRegulator.DeliverCardReward"/>
    public void DeliverCardReward()
    {
        if (Reward == null)
            return;

        _currentGame.Players[PlayerTurn].AddCard(Reward);
        Reward = null;
    }
    /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>
    public async Task<SerializedData[]> GetBinarySerials()
    {
        int numRewards = 0;
        List<SerializedData> rewardData = [];
        if (Reward != null) {
            rewardData.AddRange(await Reward.GetBinarySerials());
            numRewards = 1;
        }
        else rewardData = [];

        SerializedData[] saveData = [
            new(typeof(int), [_actionsCounter]),
            new(typeof(int), [_prevActionCount]),
            new(typeof(int), [CurrentActionsLimit]),
            new(typeof(int), [numRewards]),
            ..rewardData
        ];

        return saveData;
    }
    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try {
            _actionsCounter = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            _prevActionCount = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            CurrentActionsLimit = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            int numRewards = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            if (numRewards == 0)
                Reward = null;
            else {
                string cardTypeName = reader.ReadString();
                if (_currentGame?.Cards?.CardFactory.BuildCard(cardTypeName) is not ICard rewardCard) {
                    throw new InvalidDataException("While loading Regulator, construction of the reward card failed");
                }
                rewardCard.LoadFromBinary(reader);
                Reward = rewardCard;
            }
        } catch (Exception ex) {
            _logger.LogError("An exception was thrown while loading {Regulator}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
}

