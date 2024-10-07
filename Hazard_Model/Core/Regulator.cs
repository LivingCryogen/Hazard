using Hazard_Model.EventArgs;
using Hazard_Share.Enums;
using Hazard_Share.Interfaces.Model;
using Hazard_Share.Services.Serializer;
using Microsoft.Extensions.Logging;

namespace Hazard_Model.Core;

/// <inheritdoc cref="IRegulator"/>
public class Regulator(ILogger<Regulator> logger) : IRegulator
{
    private Game? _currentGame = null;
    private StateMachine? _machine = null;
    private readonly ILogger _logger = logger;
    private int _numPlayers = 0;
    private int _actionsCounter = 0;
    private int _prevActionCount = 0;

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

    #region Methods
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
    /// <inheritdoc cref="IRegulator.Initialize(IGame)"/>
    public void Initialize(IGame game)
    {
        _currentGame = (Game)game;
        _numPlayers = _currentGame.Players.Count;
        _machine = _currentGame.State;

        if (_currentGame.Values.SetupActionsPerPlayers.TryGetValue(_numPlayers, out int actions))
            CurrentActionsLimit = actions;
        else CurrentActionsLimit = 0;

        if (_currentGame.State.CurrentPhase == GamePhase.TwoPlayerSetup) {
            _currentGame.TwoPlayerAutoSetup();
            _prevActionCount = _actionsCounter;
        }

        _machine.StateChanged += HandleStateChanged;
    }
    /// <inheritdoc cref="IRegulator.Initialize(IGame, object?[])"/>
    public void Initialize(IGame game, object?[] loadedValues)
    {
        _currentGame = (Game)game;
        _numPlayers = _currentGame.Players!.Count;

        _machine = _currentGame.State;
        _machine!.StateChanged += HandleStateChanged;

        if (loadedValues != null) {
            _actionsCounter = (int)(loadedValues?[0] ?? 0);
            _prevActionCount = (int)(loadedValues?[1] ?? 0);
            CurrentActionsLimit = (int)(loadedValues?[2] ?? 0);
            if (((int?)loadedValues?[3] ?? 0) == 1)
                Reward = (ICard)loadedValues![4]!;
        }
    }
    private void IncrementAction()
    {
        _actionsCounter++;
        if (_currentGame.State.CurrentPhase == GamePhase.DefaultSetup) {
            if (_actionsCounter >= _currentGame.Board.Geography.NumTerritories && !_machine.PhaseStageTwo)
                _machine.PhaseStageTwo = true;
        }
        else if (_currentGame.State.CurrentPhase == GamePhase.Move) {
            if (!_currentGame.State.PhaseStageTwo)
                _currentGame.State.PhaseStageTwo = true;
        }

        if (_actionsCounter >= CurrentActionsLimit)
            ActionLimitHit();
    }
    private void ActionLimitHit()
    {
        if (_machine!.CurrentPhase == GamePhase.DefaultSetup || _machine!.CurrentPhase == GamePhase.TwoPlayerSetup)
            _machine.IncrementRound();
        else
            _machine.IncrementPhase();
    }
    /// <inheritdoc cref="IRegulator.ClaimOrReinforce(TerrID)"/>
    public void ClaimOrReinforce(TerrID territory)
    {
        if (_machine?.CurrentPhase == GamePhase.DefaultSetup) {
            _currentGame.Players[_machine.PlayerTurn].ArmyPool--;

            if (!_machine.PhaseStageTwo) {
                _currentGame.Board.Claims(_machine.PlayerTurn, territory);
                _currentGame.Players[_machine.PlayerTurn].AddTerritory(territory);
            }
            else
                _currentGame.Board.Reinforce(territory);

            IncrementAction();

            if (_machine.CurrentPhase == GamePhase.DefaultSetup)
                _machine.IncrementPlayerTurn();
        }
        else if (_machine.CurrentPhase == GamePhase.TwoPlayerSetup) {
            _currentGame.Board.Reinforce(territory);
            IncrementAction();

            int actDiff = _actionsCounter - _prevActionCount;
            switch (actDiff) {
                case 1:
                    _currentGame.Players[_machine.PlayerTurn].ArmyPool--;
                    break;
                case 2:
                    _currentGame.Players[_machine.PlayerTurn].ArmyPool--;
                    _machine.PhaseStageTwo = true;
                    break;
                case 3:
                    if (_machine.CurrentPhase == GamePhase.TwoPlayerSetup) {
                        _machine.PhaseStageTwo = false;
                        _machine.IncrementPlayerTurn();
                        _prevActionCount = _actionsCounter;
                    }
                    break;
            }
        }
        else if (_machine.CurrentPhase == GamePhase.Place) {
            _currentGame.Players[_machine.PlayerTurn].ArmyPool--;
            _currentGame.Board.Reinforce(territory);
            IncrementAction();
        }
    }
    /// <inheritdoc cref="IRegulator.MoveArmies(TerrID, TerrID, int)"/>
    public void MoveArmies(TerrID source, TerrID target, int armies)
    {
        _currentGame!.Board!.Reinforce(source, -armies);
        _currentGame.Board!.Reinforce(target, armies);

        if (_currentGame!.State!.CurrentPhase == GamePhase.Move)
            IncrementAction();
    }
    /// <inheritdoc cref="IRegulator.Battle(TerrID, TerrID, int[], int[])"/>
    public void Battle(TerrID source, TerrID target, int[] attackResults, int[] defenseResults)
    {
        if (_currentGame == null || _currentGame.Players == null || _currentGame.Board == null || _currentGame.Cards == null || _currentGame.Cards.GameDeck == null)
            throw new ArgumentNullException($"{_currentGame} or one of its properties were null when .Battle() was called");

        _actionsCounter++;

        List<int> sortedAttackResults = [.. attackResults.OrderDescending()];
        List<int> sortedDefenseResults = [.. defenseResults.OrderDescending()];

        int sourceLoss = 0;
        int targetLoss = 0;

        for (int i = 0; i < sortedAttackResults.Count; i++) {
            if (sortedDefenseResults.Count - 1 >= i) {
                if (sortedAttackResults[i] > sortedDefenseResults[i])
                    targetLoss++;
                else
                    sourceLoss++;
            }
        }

        if (targetLoss >= _currentGame!.Board!.Armies[target]) {
            int conqueredOwner = _currentGame.Board.TerritoryOwner[target];
            int newOwner = _currentGame.Board.TerritoryOwner[source];
            if (conqueredOwner > -1)
                _currentGame.Players![conqueredOwner].RemoveTerritory(target);
            _currentGame.Players[newOwner].AddTerritory(target);

            _currentGame!.Board.Conquer(source, target, _currentGame!.Board.TerritoryOwner[source]);

            Reward ??= _currentGame!.Cards.GameDeck.DrawCard();
        }
        if (sourceLoss > 0)
            _currentGame.Board.Reinforce(source, -sourceLoss);
        if (targetLoss > 0)
            _currentGame.Board.Reinforce(target, -targetLoss);
    }
    /// <inheritdoc cref="IRegulator.CanTradeInCards(int, int[])"/>
    public bool CanTradeInCards(int playerNum, int[] handIndices)
    {
        if (_currentGame == null || _currentGame.State == null) return false;

        if (playerNum != _currentGame.State.PlayerTurn)
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

        var currentPlayer = _currentGame!.Players[playerNum];

        _machine!.IncrementNumTrades(1);
        int tradebonus = _currentGame.Values!.CalculateBaseTradeInBonus(_machine.NumTrades);
        currentPlayer.GetsTradeBonus(tradebonus);
        CurrentActionsLimit += tradebonus;
        ForceDiscard((Player)currentPlayer, handIndices);

        var tradedTargets = selectedCards.SelectMany(item => item.Target);
        var controlledTargets = currentPlayer.GetControlledTargets(tradedTargets.ToArray());
        if (controlledTargets.Length == 1)
            _currentGame.Board!.Reinforce(controlledTargets[0], _currentGame.Values.TerritoryTradeInBonus);
        else if (controlledTargets.Length > 1)
            PromptBonusChoice?.Invoke(this, [.. controlledTargets]);
    }
    /// <inheritdoc cref="IRegulator.AwardTradeInBonus(TerrID)"/>
    public void AwardTradeInBonus(TerrID territory)
    {
        _currentGame!.Board!.Reinforce(territory, _currentGame.Values!.TerritoryTradeInBonus);
    }
    /// <inheritdoc cref="IRegulator.DeliverCardReward"/>
    public void DeliverCardReward()
    {
        if (Reward != null) {
            _currentGame!.Players[_machine!.PlayerTurn].AddCard(Reward);
            Reward = null;
        }
    }
    private void HandleStateChanged(object? sender, string propName)
    {
        if (propName == "CurrentPhase") {
            _prevActionCount = _actionsCounter;
            switch (_machine!.CurrentPhase) {
                case GamePhase.Place:
                    CurrentActionsLimit = _actionsCounter;
                    _currentGame!.Players![_machine.PlayerTurn].ArmyPool += _currentGame!.Players![_machine.PlayerTurn].ArmyBonus;
                    CurrentActionsLimit += _currentGame!.Players![_machine!.PlayerTurn].ArmyPool;
                    if (_currentGame.Players[_machine.PlayerTurn].HasCardSet) {
                        bool force;
                        if (_currentGame.Players[_machine.PlayerTurn].Hand.Count >= 5)
                            force = true;
                        else force = false;
                        PromptTradeIn?.Invoke(this, new PromptTradeEventArgs(_machine.PlayerTurn, force));
                    }
                    break;

                case GamePhase.Attack:
                    _currentGame!.State!.PhaseStageTwo = false;
                    break;

                case GamePhase.Move:
                    CurrentActionsLimit = _actionsCounter + 1;
                    break;
            }
        }
    }
    private ICard[]? GetCardsFromHand(int playerNum, int[] handIndices)
    {
        if (_currentGame == null || _currentGame.Cards == null)
            return null;
        if (_currentGame.Players == null || _currentGame.Players[playerNum] == null || _currentGame.Players[playerNum].Hand == null)
            return null;

        List<ICard> selectedCards = [];
        foreach (int index in handIndices) {
            var indCard = _currentGame.Players[playerNum].Hand[index];
            if (_currentGame.Players[playerNum].Hand.Count - 1 < index || indCard == null)
                return null;

            selectedCards.Add(indCard);
        }

        return [.. selectedCards];
    }
    private void ForceDiscard(Player player, int[] handIndices)
    {
        if (_currentGame == null || _currentGame.Cards == null || _currentGame.Cards.GameDeck == null) {
            _logger.LogWarning("An attempt was made to force Player {PlayerNumber} to discard cards, but a GameDeck reference could not be resolved.", player.Number);
            return;
        }
        Array.Sort(handIndices);
        Array.Reverse(handIndices); // If the indices remain in ascending order, .RemoveAt() will improperly affect the list in subsequent iterations
        foreach (int discardIndex in handIndices) {
            _currentGame.Cards.GameDeck.Discard(player.Hand[discardIndex]);
            player.RemoveCard(discardIndex);
        }
    }
    #endregion
}

