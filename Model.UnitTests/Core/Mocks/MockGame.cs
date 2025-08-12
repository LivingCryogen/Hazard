using Microsoft.Extensions.Logging;
using Microsoft.Testing.Platform.Logging;
using Model.Core;
using Model.Entities;
using Model.Stats.Services;
using Model.Tests.Core.Stubs;
using Model.Tests.DataAccess.Stubs;
using Model.Tests.Entities.Mocks;
using Model.Tests.Fixtures;
using Model.Tests.Fixtures.Mocks;
using Model.Tests.Fixtures.Stubs;
using Shared.Geography;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;

namespace Model.Tests.Core.Mocks;

public class MockGame : IGame
{
    private readonly LoggerStubT<MockGame> _logger = new();

    public MockGame()
    {
        ID = new Guid();
        Players = [
            new MockPlayer(0, Cards.CardFactory, new LoggerStubT<MockPlayer>()),
            new MockPlayer(1, Cards.CardFactory, new LoggerStubT<MockPlayer>())
        ];
        State = new StateMachine(Players.Count, new LoggerStubT<StateMachine>());
        Regulator = new MockRegulator(new LoggerStubT<MockRegulator>(), this);
        Regulator.Initialize();
        AssetFetcher = new AssetFetcherStub();
    }

    public Microsoft.Extensions.Logging.ILogger<MockGame> Logger { get => _logger; }
    public IAssetFetcher AssetFetcher { get; }
    public IBoard Board { get; set; } = new MockBoard();
    public IRegulator Regulator { get; set; }
    public IRuleValues Values { get; set; } = new MockRuleValues();
    public IStatTracker StatTracker { get; set; } = new StatTrackerStub();
    public Guid ID { get; set; }
    public bool DefaultCardMode { get; set; } = true;
    public List<IPlayer> Players { get; set; }
    public StateMachine State { get; set; } = new(2, new LoggerStubT<StateMachine>());
    public ICardBase Cards { get; set; } = new MockCardBase(SharedRegister.Registry);
    public string? SavePath { get; set; } = null;


#pragma warning disable CS0414 // For unit-testing, these are unused.
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
        Board.TerritoryOwner.Clear();
        Board.ContinentOwner.Clear();
        ((MockRegulator)Regulator).Wipe();
    }
    public void AutoBoard()
    {
        if (Board == null) return;
        if (Players.Count != 2) return;

        // Distribute all initial territories between the two players and a "dummy AI" player randomly
        int numTerritories = BoardGeography.NumTerritories / 3;
        int[] playerPool = [numTerritories, numTerritories, numTerritories];
        Random rand = new();
        byte poolsEmpty = 0b000; // bitwise flags
        byte[] masks = [0b001, 0b010, 0b100]; // flag bitwise manipulators

        for (int i = 0; i < BoardGeography.NumTerritories; i++)
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
                Board.Claims(player, (TerrID)i, 1);
                Players[player].AddTerritory((TerrID)i);
                playerPool[player]--;
                if (playerPool[player] <= 0)
                    poolsEmpty |= masks[player];  // If a player's pool is emptied, trip the PoolsEmpty flag at the appropriate bit 
            }
            else if (player == 2)
            {
                Board.Claims(-1, (TerrID)i, 1);
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
        return await Task.Run(async () =>
        {
            List<SerializedData> saveData = [];
            if (this.ID is Guid gameID)
            {
                saveData.Add(new(typeof(int), 1));
                saveData.Add(new(typeof(string), gameID.ToString()));
            }
            else
                saveData.Add(new(typeof(int), 0));
            if (Board != null)
                saveData.AddRange(await Board.GetBinarySerials());
            if (Cards != null)
                saveData.AddRange(await Cards.GetBinarySerials());
            saveData.Add(new(typeof(int), Players.Count));
            foreach (IPlayer player in Players)
                if (player != null)
                    saveData.AddRange(await player.GetBinarySerials());
            if (State != null)
                saveData.AddRange(await State.GetBinarySerials());
            saveData.AddRange(await StatTracker.GetBinarySerials());

            return saveData.ToArray();
        });
    }
    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try
        {
            bool hasGuid = (int)BinarySerializer.ReadConvertible(reader, typeof(int)) == 1;
            if (hasGuid)
                this.ID = Guid.Parse((string)BinarySerializer.ReadConvertible(reader, typeof(string)));
            Board.LoadFromBinary(reader);
            Cards.LoadFromBinary(reader);
            int numPlayers = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            Players.Clear();
            for (int i = 0; i < numPlayers; i++)
            {
                Player newPlayer = new(i, numPlayers, Cards.CardFactory, Values, Board, LoggerFactoryStub.CreateLogger<Player>());
                newPlayer.LoadFromBinary(reader);
                Cards.MapCardsToSets([.. newPlayer.Hand]);
                Players.Add(newPlayer);
            }
            State.LoadFromBinary(reader);
            StatTracker.LoadFromBinary(reader);
        }
        catch (Exception ex)
        {
            Logger.LogError("An exception was thrown while loading {Regulator}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }

    void IGame.UpdatePlayerNames(string[] names)
    {
        throw new NotImplementedException();
    }

}
