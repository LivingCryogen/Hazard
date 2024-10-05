using Hazard_Model.Tests.Entities.Mocks;
using Hazard_Model.Tests.Fixtures.Mocks;
using Hazard_Share.Enums;
using Hazard_Share.Interfaces.Model;
using Hazard_Share.Services.Serializer;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Hazard_Model.Tests.Core.Mocks;

public class MockRegulator(ILogger logger) : IRegulator
{
    private MockGame? _currentGame = null;
    private ILogger _logger = logger;
    private int _numPlayers = 0;
    private int _actionsCounter = 3;
    private int _prevActionCount = 4;
    public int CurrentActionsLimit { get; set; } = 5;
    public int PhaseActions { get; set; } = 1;
    public ICard? Reward { get; set; }

#pragma warning disable CS0414 // For unit-testing, these are unused. If integration tests are built, they should be, at which time these warnings should be re-enabled.
    public event EventHandler<TerrID[]>? PromptBonusChoice = null;
    public event EventHandler<IPromptTradeEventArgs>? PromptTradeIn = null;
#pragma warning restore CS0414
    public async Task<SerializedData[]> GetBinarySerials()
    {
        int numRewards = 0;
        List<SerializedData> rewardData = [];
        if (Reward != null) {
            rewardData.Add(new(typeof(string), [Reward.TypeName]));
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
                string cardTypeName = (string)BinarySerializer.ReadConvertible(reader, typeof(string));
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
    public void AwardTradeInBonus(TerrID territory)
    {
        throw new NotImplementedException();
    }

    public void Battle(TerrID source, TerrID target, int[] attackResults, int[] defenseResults)
    {
        throw new NotImplementedException();
    }

    public bool CanTradeInCards(int player, int[] handIndices)
    {
        throw new NotImplementedException();
    }

    public void DeliverCardReward()
    {
        throw new NotImplementedException();
    }

    public void Initialize(IGame game)
    {
        _currentGame = (MockGame)game;
        _logger = ((MockGame)game).Logger;
        _numPlayers = _currentGame.Players!.Count;

        CurrentActionsLimit = _currentGame!.Values!.SetupActionsPerPlayers![_numPlayers];

        if (_currentGame!.State!.CurrentPhase == GamePhase.TwoPlayerSetup) {
            _currentGame!.AutoBoard();
            _prevActionCount = _actionsCounter;
        }
    }

    public void Initialize(IGame game, object?[] loadedValues)
    {
        _currentGame = (MockGame)game;
        _numPlayers = _currentGame.Players!.Count;

        if (loadedValues != null) {
            _actionsCounter = (int)(loadedValues?[0] ?? 0);
            _prevActionCount = (int)(loadedValues?[1] ?? 0);
            CurrentActionsLimit = (int)(loadedValues?[2] ?? 0);
            if (((int?)loadedValues?[3] ?? 0) == 1)
                Reward = (ICard)loadedValues![4]!;
        }
    }

    public void MoveArmies(TerrID source, TerrID target, int armies)
    {
        throw new NotImplementedException();
    }

    public void ClaimOrReinforce(TerrID territory)
    {
        throw new NotImplementedException();
    }

    public void TradeInCards(int player, int[] handIndices)
    {
        throw new NotImplementedException();
    }

    public void Wipe()
    {
        _actionsCounter = 0;
        _prevActionCount = 0;
        CurrentActionsLimit = 0;
        Reward = null;
    }
}
