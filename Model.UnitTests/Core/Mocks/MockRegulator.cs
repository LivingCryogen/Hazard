using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;

namespace Model.Tests.Core.Mocks;

public class MockRegulator(ILogger logger, MockGame currentGame) : IRegulator
{
    private MockGame _currentGame = currentGame;
    private readonly ILogger _logger = logger;
    private int _numPlayers = currentGame.Players.Count;
    private int _actionsCounter = 3;
    private int _prevActionCount = 4;
    public int CurrentActionsLimit { get; set; } = 5;
    public int PhaseActions { get; set; } = 1;
    public bool RewardPending { get; set; } = true;

#pragma warning disable CS0414 // For unit-testing, these are unused. If integration tests are built, they should be, at which time these warnings should be re-enabled.
    public event EventHandler<TerrID[]>? PromptBonusChoice = null;
    public event EventHandler<IPromptTradeEventArgs>? PromptTradeIn = null;
#pragma warning restore CS0414
    /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>
    public Task<SerializedData[]> GetBinarySerials()
    {
        SerializedData[] saveData = [
            new(typeof(int), [_actionsCounter]),
            new(typeof(int), [_prevActionCount]),
            new(typeof(int), [CurrentActionsLimit]),
            new(typeof(bool), [RewardPending]),
        ];

        return Task.FromResult(saveData);
    }
    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try
        {
            _actionsCounter = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            _prevActionCount = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            CurrentActionsLimit = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            RewardPending = (bool)BinarySerializer.ReadConvertible(reader, typeof(bool));
        }
        catch (Exception ex)
        {
            _logger.LogError("An exception was thrown while loading {Regulator}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
    public void AwardTradeInBonus(TerrID territory)
    {
        throw new NotImplementedException();
    }

    public void Battle(TerrID source, TerrID target, (int AttackRoll, int DefenseRoll)[] diceRolls, int numAttackDice)
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

    public void Initialize()
    {
        CurrentActionsLimit = _currentGame.Values.SetupActionsPerPlayers[_numPlayers];

        if (_currentGame?.State?.CurrentPhase == GamePhase.TwoPlayerSetup)
        {
            // _currentGame?.AutoBoard(); For MockGame, we don't need to arrange the Board
            _prevActionCount = _actionsCounter;
        }
    }

    public void Initialize(IGame game, object?[] loadedValues)
    {
        _currentGame = (MockGame)game;
        _numPlayers = _currentGame.Players!.Count;

        if (loadedValues != null)
        {
            _actionsCounter = (int)(loadedValues?[0] ?? 0);
            _prevActionCount = (int)(loadedValues?[1] ?? 0);
            CurrentActionsLimit = (int)(loadedValues?[2] ?? 0);
            RewardPending = (bool?)loadedValues?[3] ?? false;
        }
    }

    public void MoveArmies(TerrID source, TerrID target, int armies)
    {
        throw new NotImplementedException();
    }

    public bool CanSelectTerritory(TerrID newSelected, TerrID oldSelected)
    {
        throw new NotImplementedException();
    }

    public (TerrID Selection, bool RequestInput, int? MaxValue) SelectTerritory(TerrID selected, TerrID priorSelected)
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
        RewardPending = false;
    }
}
