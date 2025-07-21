using Microsoft.Extensions.Logging;
using Model.Core;
using Model.Tests.Stats;
using Shared.Enums;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;
using Model.Tests.Fixtures.Mocks;
using System.Reflection.PortableExecutable;

namespace Model.Tests.Core.Mocks;

public class MockRegulator(ILogger logger, MockGame currentGame) : IRegulator<MockTerrID, MockContID>
{
    private MockGame _currentGame = currentGame;
    private readonly MockStatTracker _statTracker = (MockStatTracker)currentGame.StatTracker;
    private readonly ILogger _logger = logger;
    private int _numPlayers = currentGame.Players.Count;
    private int _actionsCounter = 3;
    private int _prevActionCount = 4;
    public int CurrentActionsLimit { get; set; } = 5;
    public int PhaseActions { get; set; } = 1;
    public ICard<MockTerrID>? Reward { get; set; }

#pragma warning disable CS0414 // For unit-testing, these are unused. If integration tests are built, they should be, at which time these warnings should be re-enabled.
    public event EventHandler<MockTerrID[]>? PromptBonusChoice = null;
    public event EventHandler<IPromptTradeEventArgs>? PromptTradeIn = null;
#pragma warning restore CS0414
    public async Task<SerializedData[]> GetBinarySerials()
    {
        int numRewards = 0;
        List<SerializedData> rewardData = [];
        if (Reward != null)
        {
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
        try
        {
            _actionsCounter = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            _prevActionCount = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            CurrentActionsLimit = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            int numRewards = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            if (numRewards == 0)
                Reward = null;
            else
            {
                string cardTypeName = reader.ReadString();
                if (_currentGame?.Cards?.CardFactory.BuildCard(cardTypeName) is not ICard<MockTerrID> rewardCard)
                {
                    throw new InvalidDataException("While loading Regulator, construction of the reward card failed");
                }
                rewardCard.LoadFromBinary(reader);
                Reward = rewardCard;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("An exception was thrown while loading {Regulator}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
    public void AwardTradeInBonus(MockTerrID territory)
    {
        throw new NotImplementedException();
    }

    public void Battle(MockTerrID source, MockTerrID target, (int AttackRoll, int DefenseRoll)[] diceRolls)
    {
        _actionsCounter++;

        bool conquered = false;
        MockContID? flipped = null;
        int attacker = _currentGame.Board.TerritoryOwner[source];
        int defender = _currentGame.Board.TerritoryOwner[target];
        bool retreated = false;

        int sourceLoss = 0;
        int targetLoss = 0;
        foreach (var (AttackRoll, DefenseRoll) in diceRolls)
            if (AttackRoll > DefenseRoll)
                targetLoss++;
            else
                sourceLoss++;

        if (targetLoss >= _currentGame.Board.Armies[target])
        {
            int conqueredOwner = _currentGame.Board.TerritoryOwner[target];
            int newOwner = _currentGame.Board.TerritoryOwner[source];
            if (conqueredOwner > -1)
                _currentGame.Players[conqueredOwner].RemoveTerritory(target);
            _currentGame.Players[newOwner].AddTerritory(target);

            _currentGame.Board.Conquer(source, target, _currentGame.Board.TerritoryOwner[source], out flipped);
            conquered = true;

            Reward ??= _currentGame.Cards.GameDeck.DrawCard();
        }
        if (sourceLoss > 0)
        {
            _currentGame.Board.Reinforce(source, -sourceLoss);
            if (_currentGame.Board.Armies[source] <= 1)
                retreated = true;
        }
        if (targetLoss > 0)
        {
            _currentGame.Board.Reinforce(target, -targetLoss);
        }

        _statTracker.RecordAttackAction(
            source,
            target,
            flipped,
            attacker,
            defender,
            sourceLoss,
            targetLoss,
            retreated,
            conquered);
    }

    public bool CanTradeInCards(int player, int[] handIndices)
    {
        throw new NotImplementedException();
    }

    public void DeliverCardReward()
    {
        throw new NotImplementedException();
    }

    public void Initialize()
    {
        CurrentActionsLimit = _currentGame.Values.SetupActionsPerPlayers[_numPlayers];

        if (_currentGame?.State?.CurrentPhase == GamePhase.TwoPlayerSetup)
        {
            // _currentGame?.AutoBoard(); For MockGame, we don't need to arrange the Board
            _prevActionCount = _actionsCounter;
        }
    }

    public void Initialize(IGame<TerrID, ContID> game, object?[] loadedValues)
    {
        _currentGame = (MockGame)game;
        _numPlayers = _currentGame.Players!.Count;

        if (loadedValues != null)
        {
            _actionsCounter = (int)(loadedValues?[0] ?? 0);
            _prevActionCount = (int)(loadedValues?[1] ?? 0);
            CurrentActionsLimit = (int)(loadedValues?[2] ?? 0);
            if (((int?)loadedValues?[3] ?? 0) == 1)
                Reward = (ICard<MockTerrID>)loadedValues![4]!;
        }
    }

    public void MoveArmies(MockTerrID source, MockTerrID target, int armies)
    {
        int player = _currentGame.Board.TerritoryOwner[source];
        int max = _currentGame.Board.Armies[source] - 1;

        _currentGame.Board.Reinforce(source, -armies);
        _currentGame.Board.Reinforce(target, armies);

        _statTracker.RecordMoveAction(source, target, armies == max, player);
    }

    public bool CanSelectTerritory(MockTerrID newSelected, MockTerrID oldSelected)
    {
        throw new NotImplementedException();
    }

    public (MockTerrID Selection, bool RequestInput, int? MaxValue) SelectTerritory(MockTerrID selected, MockTerrID priorSelected)
    {
        throw new NotImplementedException();
    }

    public void ClaimOrReinforce(MockTerrID territory)
    {
        throw new NotImplementedException();
    }

    public void TradeInCards(int player, int[] handIndices)
    {
        var selectedCards = GetCardsFromHand(player, handIndices);
        if (selectedCards == null || selectedCards.Length == 0)
            return;

        var currentPlayer = _currentGame.Players[player];
        _currentGame.State.IncrementNumTrades(1);
        int tradebonus = _currentGame.Values.CalculateBaseTradeInBonus(_currentGame.State.NumTrades);
        currentPlayer.GetsTradeBonus(tradebonus);
        CurrentActionsLimit += tradebonus;
        ForceDiscard((MockPlayer)currentPlayer, handIndices);

        var tradedTargets = selectedCards.SelectMany(item => item.Target);
        var controlledTargets = currentPlayer.GetControlledTargets([.. tradedTargets]);
        bool occupyBonus = false;
        if (controlledTargets.Length == 1)
            _currentGame.Board.Reinforce(controlledTargets[0], _currentGame.Values.TerritoryTradeInBonus);
        else if (controlledTargets.Length > 1)
        {
            PromptBonusChoice?.Invoke(this, [.. controlledTargets]);
            occupyBonus = true;
        }

        _statTracker.RecordTradeAction([.. tradedTargets], tradebonus, occupyBonus ? 2 : 0, player);
    }

    private ICard<MockTerrID>[] GetCardsFromHand(int playerNum, int[] handIndices)
    {
        var player = _currentGame.Players[playerNum];
        List<ICard<MockTerrID>> selectedCards = [];
        if (handIndices.Length > player.Hand.Count)
            throw new IndexOutOfRangeException($"An attempt was made to get {handIndices.Length} cards from {player}'s hand, but they only had {player.Hand.Count}.");

        foreach (int index in handIndices)
        {
            var indCard = _currentGame.Players[playerNum].Hand[index];
            selectedCards.Add(indCard);
        }

        return [.. selectedCards];
    }
    private void ForceDiscard(MockPlayer player, int[] handIndices)
    {
        // If the indices remain in ascending order, .RemoveAt() will improperly affect the list in subsequent iterations
        Array.Sort(handIndices);
        Array.Reverse(handIndices);
        foreach (int discardIndex in handIndices)
        {
            _currentGame.Cards.GameDeck.Discard(player.Hand[discardIndex]);
            player.RemoveCard(discardIndex);
        }
    }
    public void Wipe()
    {
        _actionsCounter = 0;
        _prevActionCount = 0;
        CurrentActionsLimit = 0;
        Reward = null;
    }
}
