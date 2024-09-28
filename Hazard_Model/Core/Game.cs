using Hazard_Model.DataAccess;
using Hazard_Model.Entities;
using Hazard_Share.Enums;
using Hazard_Share.Interfaces.Model;
using Hazard_Share.Services.Registry;
using Hazard_Share.Services.Serializer;
using Microsoft.Extensions.Logging;

namespace Hazard_Model.Core;


/// <summary>
/// The top-level class for an individual Game.
/// </summary>
/// <param name="values">An <see cref="IRuleValues"/>; required for game-rule constants and calculators.</param>
/// <param name="board">An <see cref="IBoard"/> storing data and relations between Board objects. Necessary for tracking game state.</param>
/// <param name="regulator">An <see cref="IRegulator"/> enforcing game-rule logic in response to player actions. The 'facade' for the input side of the VM.</param>
/// <param name="logger">An <see cref="ILogger{Game}"/> for logging debug information, warnings, errors.</param>
/// <param name="assetFetcher">An <see cref="IAssetFetcher"/> connecting the Model and the DAL through bespoke methods. Provides assets to <see cref="Game"/> properties, eg: <example><see cref="IAssetFetcher.FetchCardSets"/> for <see cref="Game.Cards"/></example>.</param>
/// <param name="typeRegister">An <see cref="ITypeRegister{ITypeRelations}"/> serving as an Application Registry. Simplifies asset loading and configuration extension. Required for operation of <see cref="ICard"/>'s default methods.</param>
public class Game(IRuleValues values, IBoard board, IRegulator regulator, ILoggerFactory loggerFactory, IAssetFetcher assetFetcher, ITypeRegister<ITypeRelations> typeRegister) : IGame
{
    private readonly IAssetFetcher _assetFetcher = assetFetcher;
    private readonly ITypeRegister<ITypeRelations> _typeRegister = typeRegister;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    /// <inheritdoc cref="IGame.ID"/>.
    public Guid ID { get; set; } 
    private string? VMSaveData { get; set; } = null;
    /// <inheritdoc cref="IGame.DefaultCardMode"/>
    public bool DefaultCardMode { get; set; } = true; // future implementation of Mission Cards or other ICard extensions would hinge on this being set to false
    /// <summary>
    /// Gets or sets a logger for  debug information and errors. Should be provided by the DI system.
    /// </summary>
    /// <value>An implementation of <see cref="ILogger{T}"/>.</value>
    public ILogger Logger { get; private set; } = loggerFactory.CreateLogger<Game>();
    /// <inheritdoc cref="IGame.Values"/>.
    public IRuleValues Values { get; set; } = values;
    /// <inheritdoc cref="IGame.Board"/>.
    public IBoard Board { get; set; } = board;
    /// <inheritdoc cref="IGame.Regulator"/>.
    public IRegulator Regulator { get; set; } = regulator;
    /// <inheritdoc cref="IGame.State"/>.
    public StateMachine State { get; set; } = new(0, loggerFactory.CreateLogger<StateMachine>());
    /// <inheritdoc cref="IGame.Cards"/>.
    public CardBase Cards { get; set; } = new(loggerFactory.CreateLogger<CardBase>(), typeRegister);
    /// <inheritdoc cref="IGame.Players"/>.
    public List<IPlayer> Players { get; set; } = [];
    /// <inheritdoc cref="IGame.PlayerLost"/>.
    public event EventHandler<int>? PlayerLost;
    /// <inheritdoc cref="IGame.PlayerWon"/>.
    public event EventHandler<int>? PlayerWon;
    /// <inheritdoc cref="IGame.Initialize(string[])"/>.
    public void Initialize(string[] names, string? fileName)
    {
        ID = Guid.NewGuid();
        int numPlayers = names.Length;
        State = new(numPlayers, _loggerFactory.CreateLogger<StateMachine>());
        Cards = new(Logger, _typeRegister);
        if (numPlayers > 1) {
            for (int i = 0; i < numPlayers; i++) {
                Players!.Add(new Player(names[i], i, numPlayers, Cards.CardFactory, Values, Board, _loggerFactory.CreateLogger<Player>()));
                Players.Last().PlayerLost += OnPlayerLost;
                Players.Last().PlayerWon += OnPlayerWin;
            }
        }
        Cards.Initialize(_assetFetcher, DefaultCardMode);
        Regulator.Initialize(this);

        if (fileName != null)
            BinarySerializer.Load([this], fileName);
    }
    /// <inheritdoc cref="IGame.Save"/>.
    public async Task Save(bool isNewFile, string fileName, string vMSaveData)
    {
        VMSaveData = vMSaveData; // Save data from VM/View is passed as a string and should precede all other data in the file.
        await BinarySerializer.Save([this], fileName, isNewFile);
    }
    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(() =>
        {
            List<SerializedData> saveData = [];
            if (VMSaveData != null)
                saveData.Add(new(typeof(string), [VMSaveData]));
            if (this.ID is Guid gameID)
                saveData.Add(new(typeof(string), [gameID.ToString()]));
            saveData.AddRange(Board?.GetBinarySerials().Result ?? []);
            foreach (IPlayer player in Players)
                saveData.AddRange(player?.GetBinarySerials().Result ?? []);
            saveData.AddRange(Cards?.GetBinarySerials().Result ?? []);
            saveData.AddRange(State?.GetBinarySerials().Result ?? []);
            saveData.AddRange(Regulator?.GetBinarySerials().Result ?? []);

            return saveData.ToArray();
        });
    }
    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try {
            this.ID = Guid.Parse((string)BinarySerializer.ReadConvertible(reader, typeof(string)));
            Board.LoadFromBinary(reader);
            foreach (IPlayer player in Players)
                player.LoadFromBinary(reader);
            Cards.LoadFromBinary(reader);
            State.LoadFromBinary(reader);
            Regulator.LoadFromBinary(reader);
        } catch (Exception ex) {
            Logger.LogError("An exception was thrown while loading {Regulator}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }

    /// <summary>
    /// Sets up a two-player game. Rules dictate that two-player setup includes a third, neutral, dummy "player", and that the initial selection of territories is random.
    /// </summary>
    public void TwoPlayerAutoSetup()
    {
        if (Board == null) return;
        if (Players.Count != 2) return;

        // Distribute all initial territories between the two players and a "dummy AI" player randomly
        int numTerritories = Board.Geography.NumTerritories / 3;
        int[] playerPool = [numTerritories, numTerritories, numTerritories];
        Random rand = new();
        byte poolsEmpty = 0b000; // bitwise flags
        byte[] masks = [0b001, 0b010, 0b100]; // flag bitwise manipulators

        for (int i = 0; i < Board!.Geography.NumTerritories; i++) {
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
    private void OnPlayerLost(object? sender, System.EventArgs e)
    {
        if (sender is IPlayer loser) {
            State?.DisablePlayer(loser.Number);
            foreach (ICard card in loser.Hand)
                Cards?.GameDeck?.Discard(card);
            loser.Hand.Clear();
            PlayerLost?.Invoke(this, loser.Number);
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
}

