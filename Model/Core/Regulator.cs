using Microsoft.Extensions.Logging;
using Model.EventArgs;
using Share.Enums;
using Share.Interfaces.Model;
using Share.Services.Serializer;

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

    private void HandleStateChanged(object? sender, string propName)
    {
        if (propName != "CurrentPhase")
            return;

        _prevActionCount = _actionsCounter;
        switch (_machine.CurrentPhase) {
            case GamePhase.Place:
                CurrentActionsLimit = _actionsCounter;
                _currentGame.Players[_machine.PlayerTurn].ArmyPool += _currentGame.Players[_machine.PlayerTurn].ArmyBonus;
                CurrentActionsLimit += _currentGame.Players[_machine.PlayerTurn].ArmyPool;

                if (!_currentGame.Players[_machine.PlayerTurn].HasCardSet)
                    break;

                bool force = false;
                if (_currentGame.Players[_machine.PlayerTurn].Hand.Count >= 5)
                    force = true;
                PromptTradeIn?.Invoke(this, new PromptTradeEventArgs(_machine.PlayerTurn, force));
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
        
        if (_machine.CurrentPhase == GamePhase.DefaultSetup && ActionsExceedTerritoryCount() && !_machine.PhaseStageTwo)
                _machine.PhaseStageTwo = true;
        else if (_machine.CurrentPhase == GamePhase.Move && !_machine.PhaseStageTwo)
                _machine.PhaseStageTwo = true;

        if (_actionsCounter >= CurrentActionsLimit)
            ActionLimitHit();
    }
    private bool ActionsExceedTerritoryCount()
    {
        return _actionsCounter >= _currentGame.Board.Geography.NumTerritories;
    }
    private void ActionLimitHit()
    {
        if (_machine!.CurrentPhase == GamePhase.DefaultSetup || _machine!.CurrentPhase == GamePhase.TwoPlayerSetup)
            _machine.IncrementRound();
        else
            _machine.IncrementPhase();
    }
    private void ForceDiscard(Player player, int[] handIndices)
    {
        Array.Sort(handIndices);
        Array.Reverse(handIndices); // If the indices remain in ascending order, .RemoveAt() will improperly affect the list in subsequent iterations
        foreach (int discardIndex in handIndices) {
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

        if (_machine.CurrentPhase == GamePhase.TwoPlayerSetup) {
            _prevActionCount = _actionsCounter;
            if (_currentGame is Game game)
                game.TwoPlayerAutoSetup();
        }

        _machine.StateChanged += HandleStateChanged;
    }
    /// <inheritdoc cref="IRegulator.ClaimOrReinforce(TerrID)"/>
    public void ClaimOrReinforce(TerrID territory)
    {
        switch (_machine.CurrentPhase) {
            case GamePhase.DefaultSetup:
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

                break;
            case GamePhase.TwoPlayerSetup:
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
                break;
            case GamePhase.Place:
                _currentGame.Players[_machine.PlayerTurn].ArmyPool--;
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

        if (_currentGame.State.CurrentPhase == GamePhase.Move)
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
        if (playerNum != _machine.PlayerTurn)
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
        
        _currentGame.Players[_machine.PlayerTurn].AddCard(Reward);
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

