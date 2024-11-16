using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Model.Assets;
using Model.Entities;
using Shared.Geography;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Registry;
using Shared.Services.Serializer;

namespace Model.Core;

/// <summary>
/// The top-level class for an individual Game.
/// </summary>
public class Game : IGame
{
    private readonly ITypeRegister<ITypeRelations> _typeRegister;
    private readonly ILoggerFactory _loggerFactory;
    /// <summary>
    /// Constructs a game given a number of players and DI-injected DAL, configuration, and logger.
    /// </summary>
    /// <param name="numPlayers"><para>The <see cref="int">number</see> of players for this game.</para> Used during initialization of <see cref="StateMachine"/> and <see cref="Players"/>.
    /// <br/>When loading from a file, this will be 0, and components update after calling <see cref="LoadFromBinary(BinaryReader)"/>.</param>
    /// <param name="loggerFactory">Builds configured loggers for various components when and where the DI system does not do so directly.</param>
    /// <param name="assetFetcher">Connects the Model and the DAL through bespoke methods. Provides assets to game properties, eg:<see cref="IAssetFetcher.FetchCardSets"/> for <see cref="Game.Cards"/>.</param>
    /// <param name="typeRegister">Serves as an Application Type Registry. Simplifies asset loading and configuration extension.<br/> Required for operation of <see cref="ICard"/>'s default methods and DAL operations.</param>
    /// <param name="config">Configuration provided by DI. Values derived from "View\appsettings.json."</param>
    public Game(int numPlayers, ILoggerFactory loggerFactory, IAssetFetcher assetFetcher,
        ITypeRegister<ITypeRelations> typeRegister, IConfiguration config)
    {
        AssetFetcher = assetFetcher;
        _typeRegister = typeRegister;
        _loggerFactory = loggerFactory;
        BoardGeography.Initialize(AssetFetcher.FetchGeography());
        ID = Guid.NewGuid();
        Logger = loggerFactory.CreateLogger<Game>();
        Board = new EarthBoard(config, loggerFactory.CreateLogger<EarthBoard>());
        Values = new RuleValues(config);
        State = new(numPlayers, loggerFactory.CreateLogger<StateMachine>());
        Cards = new(loggerFactory, typeRegister);
        Cards.InitializeFromAssets(AssetFetcher, DefaultCardMode);
        Players = [];
        for (int i = 0; i < numPlayers; i++) {
            Players.Add(new Player(i, State.NumPlayers, Cards.CardFactory, Values, Board, _loggerFactory.CreateLogger<Player>()));
            Players.Last().PlayerLost += OnPlayerLost;
            Players.Last().PlayerWon += OnPlayerWin;
        }
    }

    /// <inheritdoc cref="IGame.PlayerLost"/>.
    public event EventHandler<int>? PlayerLost;
    /// <inheritdoc cref="IGame.PlayerWon"/>.
    public event EventHandler<int>? PlayerWon;

    #region Properties
    /// <inheritdoc cref="IGame.AssetFetcher"/>
    public IAssetFetcher AssetFetcher { get; }
    /// <inheritdoc cref="IGame.ID"/>.
    public Guid ID { get; private set; }
    /// <inheritdoc cref="IGame.DefaultCardMode"/>
    public bool DefaultCardMode { get; private set; } = true; // future implementation of Mission Cards or other ICard extensions would hinge on this being set to false
    /// <summary>
    /// Gets or sets a logger for logging debug information and errors. Provided by the DI system indirectly via an <see cref="ILoggerFactory"/>.
    /// </summary>
    /// <value>An implementation of <see cref="ILogger{T}"/>.</value>
    public ILogger Logger { get; private set; }
    /// <inheritdoc cref="IGame.Values"/>.
    public IRuleValues Values { get; private set; }
    /// <inheritdoc cref="IGame.Board"/>
    public IBoard Board { get; private set; }
    /// <inheritdoc cref="IGame.State"/>.
    public StateMachine State { get; private set; }
    /// <inheritdoc cref="IGame.Cards"/>.
    public CardBase Cards { get; private set; }
    /// <inheritdoc cref="IGame.Players"/>.
    public List<IPlayer> Players { get; private set; }
    #endregion

    private void OnPlayerLost(object? sender, System.EventArgs e)
    {
        if (sender is IPlayer loser) {
            State?.DisablePlayer(loser.Number);
            foreach (ICard card in loser.Hand)
                Cards?.GameDeck?.Discard(card);
            loser.Hand.Clear();
            PlayerLost?.Invoke(this, loser.Number);

            // Checks if there are any other active players, if not, the last active player wins
            List<int> activePlayerIndex = [];
            for (int i = 0; i < State?.IsActivePlayer.Count; i++)
                if (State?.IsActivePlayer[i] ?? false)
                    activePlayerIndex.Add(i);
            if (activePlayerIndex.Count == 1)
                PlayerWon?.Invoke(this, activePlayerIndex[0]);

        }
        else throw new ArgumentException($"{PlayerLost} was fired but the sender was not an {nameof(IPlayer)}.", nameof(sender));
    }
    private void OnPlayerWin(object? sender, System.EventArgs e)
    {
        if (sender is IPlayer winner) {
            PlayerWon?.Invoke(this, (winner.Number));
            State?.DisablePlayer(winner.Number);
        }
        else throw new ArgumentException($"{PlayerWon} was fired but the sender was not an {nameof(IPlayer)}.", nameof(sender));
    }
    /// <inheritdoc cref="IGame.UpdatePlayerNames(string[])"/>
    public void UpdatePlayerNames(string[] names)
    {
        if (names.Length != Players.Count)
            throw new ArgumentException("The number of names provided for updating did not match the number of Players in the Game.", nameof(names));
        for (int i = 0; i < names.Length; i++)
            Players[i].Name = names[i];
    }
    /// <summary>
    /// Sets up a two-player game. Rules dictate that two-player setup includes a third, neutral, dummy "player", and that the initial selection of territories is random.
    /// </summary>
    public void TwoPlayerAutoSetup()
    {
        if (Board == null) return;
        if (Players.Count != 2) return;

        // Distribute all initial territories between the two players and a "dummy AI" player randomly
        int numTerritories = BoardGeography.NumTerritories / 3;
        int[] playerPool = [numTerritories, numTerritories, numTerritories];
        Random rand = new();
        byte poolsEmpty = 0b000; // bitwise flags
        byte[] masks = [0b001, 0b010, 0b100]; // flag bitwise manipulators

        for (int i = 0; i < BoardGeography.NumTerritories; i++) {
            // select the random player, making sure not to select a player without any selections left
            int player;
            switch (poolsEmpty) {
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

            if (player < 2 && player > -1) {
                Board.Claims(player, (TerrID)i, 1);
                Players[player].AddTerritory((TerrID)i);
                playerPool[player]--;
                if (playerPool[player] <= 0)
                    poolsEmpty |= masks[player];  // If a player's pool is emptied, trip the PoolsEmpty flag at the appropriate bit 
            }
            else if (player == 2) {
                Board.Claims(-1, (TerrID)i, 1);
                playerPool[player]--;
                if (playerPool[player] <= 0)
                    poolsEmpty |= masks[player];  // If a player's pool is emptied, trip the PoolsEmpty flag at the appropriate bit 
            }
        }
    }
    /// <inheritdoc cref="IGame.Save"/>.
    public async Task Save(bool isNewFile, string fileName)
    {
        await BinarySerializer.Save([this], fileName, isNewFile);
    }
    /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>
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
            foreach (IPlayer player in Players)
                saveData.AddRange(player?.GetBinarySerials().Result ?? []);
            saveData.AddRange(State?.GetBinarySerials().Result ?? []);

            return saveData.ToArray();
        });
    }
    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try {
            this.ID = Guid.Parse((string)BinarySerializer.ReadConvertible(reader, typeof(string)));
            Board.LoadFromBinary(reader);
            Cards.LoadFromBinary(reader);
            int numPlayers = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            Players.Clear();
            for (int i = 0; i < numPlayers; i++) {
                Player newPlayer = new(i, numPlayers, Cards.CardFactory, Values, Board, _loggerFactory.CreateLogger<Player>());
                newPlayer.LoadFromBinary(reader);
                Cards.MapCardsToSets([.. newPlayer.Hand]);
                newPlayer.PlayerLost += OnPlayerLost;
                newPlayer.PlayerWon += OnPlayerWin;
                Players.Add(newPlayer);
            }
            State.LoadFromBinary(reader);
        } catch (Exception ex) {
            Logger.LogError("An exception was thrown while loading {Regulator}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
}

