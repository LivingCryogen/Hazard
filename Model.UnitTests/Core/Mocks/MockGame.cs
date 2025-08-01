﻿using Microsoft.Extensions.Logging;
using Microsoft.Testing.Platform.Logging;
using Model.Core;
using Model.Entities;
using Model.Tests.DataAccess.Stubs;
using Model.Tests.Entities.Mocks;
using Model.Tests.Fixtures;
using Model.Tests.Fixtures.Mocks;
using Model.Tests.Fixtures.Stubs;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;

namespace Model.Tests.Core.Mocks;

public class MockGame : IGame<MockTerrID, MockContID>
{
    private readonly LoggerStubT<MockGame> _logger = new();

    public MockGame()
    {
        ID = new Guid();
        Players = [
            new MockPlayer(0, Cards.CardFactory, Board, new LoggerStubT<MockPlayer>()),
            new MockPlayer(1, Cards.CardFactory, Board, new LoggerStubT<MockPlayer>())
        ];
        State = new StateMachine(Players.Count, new LoggerStubT<StateMachine>());
        Regulator = new MockRegulator(new LoggerStubT<MockRegulator>(), this);
        Regulator.Initialize();
        AssetFetcher = new AssetFetcherStub();
    }

    public Microsoft.Extensions.Logging.ILogger<MockGame> Logger { get => _logger; }
    public IAssetFetcher<MockTerrID> AssetFetcher { get; }
    public IBoard<MockTerrID, MockContID> Board { get; set; } = new MockBoard();
    public IRegulator<MockTerrID, MockContID> Regulator { get; set; }
    public IRuleValues<MockContID> Values { get; set; } = new MockRuleValues();
    public IStatTracker<MockTerrID, MockContID> StatTracker { get; set; }
    public Guid ID { get; set; }
    public bool DefaultCardMode { get; set; } = true;
    public List<IPlayer<MockTerrID>> Players { get; set; }
    public StateMachine State { get; set; } = new(2, new LoggerStubT<StateMachine>());
    public ICardBase<MockTerrID> Cards { get; set; } = new MockCardBase(SharedRegister.Registry);


#pragma warning disable CS0414 // For unit-testing, these are unused. If integration tests are built, they should be, at which time these warnings should be re-enabled.
    public event EventHandler<int>? PlayerLost = null;
    public event EventHandler<int>? PlayerWon = null;
#pragma warning restore CS0414
    public void Wipe()
    {
        ID = Guid.Empty;
        Players.Clear();
        ((MockCardBase)Cards).Wipe();
        State = new StateMachine(2, new LoggerStubT<StateMachine>());
        Board.Armies.Clear();
        Board.ContinentOwner.Clear();
        ((MockRegulator)Regulator).Wipe();
    }
    public void AutoBoard()
    {
        if (Board == null) return;
        if (Players.Count != 2) return;

        // Distribute all initial territories between the two players and a "dummy AI" player randomly
        int numTerritories = MockGeography.NumTerritories / 3;
        int[] playerPool = [numTerritories, numTerritories, numTerritories];
        Random rand = new();
        byte poolsEmpty = 0b000; // bitwise flags
        byte[] masks = [0b001, 0b010, 0b100]; // flag bitwise manipulators

        for (int i = 0; i < MockGeography.NumTerritories; i++)
        {
            // select the random player, making sure not to select a player without any selections left
            int player;
            switch (poolsEmpty)
            {
                case 0:
                    player = rand.Next(0, 3);
                    break;
                case 0b1:
                    player = rand.Next(1, 3);
                    break;
                case 0b10:
                    player = rand.Next(0, 2);
                    if (player == 1)
                        player++;
                    break;
                case 0b100:
                    player = rand.Next(0, 2);
                    break;
                case 0b1 | 0b10:
                    player = 2;
                    break;
                case 0b1 | 0b100:
                    player = 1;
                    break;
                case 0b10 | 0b100:
                    player = 0;
                    break;
                default:
                    player = -1;
                    break;
            }

            if (player < 2 && player > -1)
            {
                Board.Claims(player, (MockTerrID)i, 1);
                Players[player].AddTerritory((MockTerrID)i);
                playerPool[player]--;
                if (playerPool[player] <= 0)
                    poolsEmpty |= masks[player];  // If a player's pool is emptied, trip the PoolsEmpty flag at the appropriate bit 
            }
            else if (player == 2)
            {
                Board.Claims(-1, (MockTerrID)i, 1);
                playerPool[player]--;
                if (playerPool[player] <= 0)
                    poolsEmpty |= masks[player];  // If a player's pool is emptied, trip the PoolsEmpty flag at the appropriate bit 
            }
        }
    }

    public void Initialize(string[] names, string? fileName, long? streamLoc)
    {
        throw new NotImplementedException();
    }

    public Task Save(bool isNewFile, string fileName)
    {
        throw new NotImplementedException();
    }

    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(() =>
        {
            List<SerializedData> saveData = [];
            if (this.ID is Guid gameID)
                saveData.Add(new(typeof(string), [gameID.ToString()]));
            saveData.AddRange(Board?.GetBinarySerials().Result ?? []);
            saveData.AddRange(Cards?.GetBinarySerials().Result ?? []);
            saveData.Add(new(typeof(int), [Players.Count]));
            foreach (IPlayer<MockTerrID> player in Players)
                saveData.AddRange(player?.GetBinarySerials().Result ?? []);
            saveData.AddRange(State?.GetBinarySerials().Result ?? []);
            saveData.AddRange(Regulator?.GetBinarySerials().Result ?? []);

            return saveData.ToArray();
        });
    }
    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try
        {
            this.ID = Guid.Parse((string)BinarySerializer.ReadConvertible(reader, typeof(string)));
            Board.LoadFromBinary(reader);
            Cards.LoadFromBinary(reader);
            int numPlayers = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            Players.Clear();
            for (int i = 0; i < numPlayers; i++)
            {
                MockPlayer newPlayer = new(i, Cards.CardFactory, Board, new LoggerStubT<MockPlayer>());
                newPlayer.LoadFromBinary(reader);
                Cards.MapCardsToSets([.. newPlayer.Hand]);
                Players.Add(newPlayer);
            }
            State = new(numPlayers, new LoggerStubT<StateMachine>());
            State.LoadFromBinary(reader);
            Regulator.LoadFromBinary(reader);
            if (Regulator.Reward is ICard<MockTerrID> rewardCard)
                Cards.MapCardsToSets([rewardCard]);
        }
        catch (Exception ex)
        {
            Logger.LogError("An exception was thrown while loading {Regulator}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }

    void IGame<MockTerrID, MockContID>.UpdatePlayerNames(string[] names)
    {
        throw new NotImplementedException();
    }

}
