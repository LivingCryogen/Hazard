using Hazard_Model.Core;
using Hazard_Model.Entities;
using Hazard_Share.Enums;
using Hazard_Share.Interfaces.Model;
using Hazard_Share.Services.Registry;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Hazard_Model.DataAccess;
/// <summary>
/// Saves and loads games.
/// </summary>
/// <remarks>
/// Uses binary encoding for serialization/deserialization. Saving is asynchronous. Loading is not.
/// </remarks>
/// <param name="game">The <see cref="IGame"/> to be saved or loaded into.</param>
/// <param name="stream">The <see cref="FileStream"/> to read or write to.</param>
/// <param name="registry">The application's registry. Necessary for the reflection-dependent default methods of <see cref="ICard"/> and any similar future functionality.</param>
/// <param name="logger">A <see cref="ILogger"/> for logging debug information.</param>
public class BinarySerializer(IGame game, FileStream stream, ITypeRegister<ITypeRelations> registry, ILogger logger)
{
    private readonly IGame _game = game;
    private readonly ILogger _logger = logger;
    private readonly ITypeRegister<ITypeRelations> _registry = registry;
    private readonly FileStream _stream = stream;
    /// <summary>
    /// Serializes <see cref="_game"/> to <see cref="_stream"/> asynchronously.
    /// </summary>
    /// <remarks>
    /// Works in two stages: on a background thread <see cref="_game"/>'s save data is prepared and written to a MemoryStream,<br/>
    /// and then it is asynchronously copied to <see cref="_stream"/>.
    /// </remarks>
    /// <param name="precedingData">Any save data from above layers -- View or ViewModel -- is put together into this string. It precedes any other data in the file.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    public async Task SaveGame(string precedingData)
    {
        List<(object? DataObj, Type? SerialType)> saveData = [];
        await Task.Run(() =>
        {
            _logger.LogInformation("Beginning save of Game {ID}.", _game.ID);

            if (!string.IsNullOrEmpty(precedingData)) { // makes testing the Serializer easier (ie, to use a test version that has no VM data, use "" as the precedingData paremeter)
                _logger.LogInformation($"Serializing color names...");
                saveData.Add((precedingData, typeof(string)));
            }
            saveData.Add((_game.ID.ToString(), typeof(string)));
            _logger.LogInformation("Serializing Board state...");
            // CHANGES HERE -- 
            if (_game.Board != null) {
                var boardData = _game.Board.GetSaveData();
                // ....
            }
            _logger.LogInformation("Serializing Players...");
            SerializePlayerStates(_game.Players, saveData);
            _logger.LogInformation("Serializing Card Base...");
            if (_game.Cards != null) {
                saveData.Add((_game.DefaultCardMode, typeof(bool)));
                SerializeCardBase(_game.Cards, saveData, _logger);
            }
            _logger.LogInformation("Serializing StateMachine...");
            if (_game.State != null)
                saveData.AddRange(_game.State.GetSaveData());
            _logger.LogInformation("Serializing Regulator...");
            if (_game.Regulator != null)
                SerializeRegulator(_game.Regulator, saveData);
        });

        using (var memoryStream = new MemoryStream()) {
            BinaryWriter writer = new(memoryStream);
            WriteData(writer, saveData, _logger);
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(_stream);
            await _stream.FlushAsync();
        }

        _stream.Dispose();
    }
    /// <summary>
    /// Writes the data provided in <paramref name="saveData"/> using a provided <paramref name="writer"/>.
    /// </summary>
    /// <remarks>
    /// The first part of each item in the savedata list is written as the primitive <see cref="Type"/> given in the second part <br/>
    /// (it is cast and then written using the appropriate method of <paramref name="writer"/>).
    /// </remarks>
    /// <param name="writer">A <see cref="BinaryWriter"/> using <see cref="_stream"/>.</param>
    /// <param name="saveData">A list of <see cref="Tuple{T1, T2}"/>, where T1 is an object containing a value to be written, and T2 is the write <see cref="Type"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> for logging debug information and errors.</param>
    public static void WriteData(BinaryWriter writer, List<(object? Datum, Type? SerialType)> saveData, ILogger logger)
    {
        foreach ((object? Datum, Type? SerialType) dataPair in saveData) {
            if (dataPair.SerialType == null) {
                logger?.LogWarning("{Pair} at position {Index} had a null value given for Serial Type and was skipped.", dataPair, saveData.IndexOf(dataPair));
                continue;
            }

            logger?.LogDebug("Writing serial of type {SerialType} for {Datum}...", dataPair.SerialType, dataPair.Datum);

            switch (dataPair.SerialType) {
                case Type t when t == typeof(byte):
                    writer.Write((byte)dataPair.Datum!);
                    break;
                case Type t when t.IsEnum:
                    writer.Write((int)dataPair.Datum!);
                    break;
                case Type t when t == typeof(int):
                    writer.Write((int)dataPair.Datum!);
                    break;
                case Type t when t == typeof(bool):
                    writer.Write((bool)dataPair.Datum!);
                    break;
                case Type t when t == typeof(string):
                    writer.Write((string)dataPair.Datum!);
                    break;
            }
        }
        saveData.Clear();
    }
    /// <summary>
    /// Serializes an <see cref="IBoard"/>.
    /// </summary>
    /// <param name="saveData">A list of <see cref="Tuple{T1, T2}"/> to which the data of <paramref name="board"/> is parsed.</param>
    /// <param name="board">The <see cref="IBoard"/> whose savedata is parsed and stored in <paramref name="saveData"/>.</param>
    public static void SerializeBoardState(IBoard board, List<(object? DataObj, Type? SerialType)> saveData)
    {
        for (int i = 0; i < board.Geography.NumTerritories; i++)
            saveData.Add((board.Armies[(TerrID)i], typeof(int)));

        for (int i = 0; i < board.Geography.NumTerritories; i++)
            saveData.Add((board.TerritoryOwner[(TerrID)i], typeof(int)));

        for (int i = 0; i < board.Geography.NumContinents; i++)
            saveData.Add((board.ContinentOwner[(ContID)i], typeof(int)));
    }
    /// <summary>
    /// Serializes a <see cref="CardBase"/>.
    /// </summary>
    /// <param name="cardBase">The <see cref="CardBase"/> to serialize to <paramref name="saveData"/>.</param>
    /// <param name="saveData">The List of <see cref="Tuple{T1, T2}"/> to which <paramref name="cardBase"/> is serialized.</param>
    /// <param name="logger">An <see cref="ILogger"/> for logging debug information and errors.</param>
    public static void SerializeCardBase(CardBase cardBase, List<(object? DataObj, Type? SerialType)> saveData, ILogger logger)
    {
        if (cardBase.Sets == null || cardBase.Sets.Count == 0) {
            logger.LogError("SerializeCardBase could not find any {SetName}s in {Base}. Check initialization.", nameof(ICardSet), cardBase);
            return;
        }
        if (cardBase.GameDeck == null) {
            logger.LogError("SerializeCardBase could not find any {Deck}s in {Base}. Check initialization.", cardBase.GameDeck, cardBase);
            return;
        }

        saveData.Add((cardBase.Sets.Count, typeof(int)));
        foreach (ICardSet cardSet in cardBase.Sets) {
            saveData.Add((cardSet.Name, typeof(string)));
        }

        int libCount = 0;
        if (cardBase.GameDeck.Library != null)
            libCount = cardBase.GameDeck.Library.Count;
        saveData.Add((libCount, typeof(int)));
        for (int i = 0; i < libCount; i++) {
            var cardInfo = cardBase.GameDeck.Library![i].GetSaveData(logger);
            SerializeCardInfo(cardInfo, saveData);
        }
        int discardCount = 0;
        if (cardBase.GameDeck.DiscardPile != null)
            discardCount = cardBase.GameDeck.DiscardPile.Count;
        saveData.Add((discardCount, typeof(int)));
        for (int i = 0; i < discardCount; i++) {
            var cardInfo = cardBase.GameDeck.DiscardPile![i].GetSaveData(logger);
            SerializeCardInfo(cardInfo, saveData);
        }
    }
    private static void SerializeCardInfo((string TypeName, string[] PropertyNames, Type[] SerialTypes, object?[]?[]? PropertySerials) cardInfo, List<(object? Datum, Type? DataType)> saveData)
    {
        saveData.Add((cardInfo.TypeName, typeof(string)));
        int numProperties = cardInfo.PropertyNames.Length;
        saveData.Add((numProperties, typeof(int)));
        for (int i = 0; i < numProperties; i++) {
            saveData.Add((cardInfo.PropertyNames[i], typeof(string)));
            if (cardInfo.PropertySerials != null) {
                var serialType = cardInfo.SerialTypes[i];
                if (cardInfo.PropertySerials[i] != null) {
                    int numSerialValues = cardInfo.PropertySerials[i]!.Length;
                    saveData.Add((numSerialValues, typeof(int)));
                    for (int j = 0; j < numSerialValues; j++)
                        saveData.Add((cardInfo.PropertySerials[i]![j], serialType));
                }
            }
        }
    }
    /// <summary>
    /// Serializes an <see cref="IRegulator"/>.
    /// </summary>
    /// <param name="regulator">The <see cref="IRegulator"/> to serialize.</param>
    /// <param name="saveData">The List of <see cref="Tuple{T1, T2}"/> to which <paramref name="regulator"/> is serialized.</param>
    /// <exception cref="ArgumentNullException">Thrown if the integer flag indicating whether a <see cref="IRegulator.Reward"/> is present in <paramref name="regulator"/> was null (could not be unboxed).</exception>
    public static void SerializeRegulator(IRegulator regulator, List<(object? DataObj, Type? SerialType)> saveData)
    {
        var regData = regulator.GetSaveData();
        var rewardData = (regData?[3].Datum) ?? throw new ArgumentNullException($"The integer flag indicating whether a Reward is present in {regulator} was null.");
        if ((int)rewardData == 0) // if a Reward is not present
            saveData.AddRange(regData);
        else if ((int)rewardData == 1) {
            saveData.AddRange(regData.GetRange(0, 4));
            SerializeCardInfo(((string TypeName, string[] PropertyNames, Type[] SerialTypes, object?[]?[]? PropertySerials))regData[4].Datum!, saveData);
        }
    }
    /// <summary>
    /// Serializes a list of <see cref="IPlayer"/>.
    /// </summary>
    /// <param name="players">The <see cref="List{T}"/> of <see cref="IPlayer"/> to be serialized.</param>
    /// <param name="saveData">The List of <see cref="Tuple{T1, T2}"/> to which <paramref name="players"/> is serialized.</param>
    public static void SerializePlayerStates(List<IPlayer> players, List<(object? DataObj, Type? SerialType)> saveData)
    {
        saveData.Add((players.Count, typeof(int)));
        foreach (IPlayer player in players) {
            List<(object? Datum, Type? DataType)> playerData = player.GetSaveData();
            saveData.Add((playerData[0].Datum, typeof(string)));
            saveData.Add((playerData[1].Datum, typeof(int)));
            saveData.Add((playerData[2].Datum, typeof(int)));
            int numControlledTerritories = (int)playerData[3].Datum!;
            saveData.Add((numControlledTerritories, typeof(int)));
            int dataIndex = 4;
            for (int i = 4; i < 4 + numControlledTerritories; i++) {
                saveData.Add((playerData[i].Datum, playerData[i].DataType));
                dataIndex++;
            }
            int numInHand = (int)playerData[dataIndex].Datum!;
            saveData.Add((numInHand, typeof(int)));
            dataIndex++;
            for (int i = dataIndex; i < dataIndex + numInHand; i++) {
                if (playerData[i].DataType == null)
                    continue;
                if (playerData[i].DataType == typeof(ICard) || playerData[i].DataType!.GetInterface(nameof(ICard)) != null)
                    SerializeCardInfo(((string TypeName, string[] PropertyNames, Type[] SerialTypes, object?[]?[]? PropertySerials))playerData[i].Datum!, saveData);
            }
        }
    }
    /// <summary>
    /// Deserializes an entire <see cref="IGame"/> from <see cref="_stream"/> to <see cref="_game"/>.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="SaveGame(string)"/>, this method is entirely synchronous.
    /// </remarks>
    public void LoadGame()
    {
        if (_game?.Board == null || _game.Players == null || _game.Cards == null || _game.Regulator == null || _game.Values == null) {
            _logger.LogError($"LoadGame() was called on a Game with a null Core feature. Check initialization.");
            return;
        }

        using BinaryReader fileReader = new(_stream);
        _game.ID = Guid.Parse(fileReader.ReadString());

        LoadBoardState(fileReader, _game.Board);
        LoadPlayerStates(fileReader, _game.Players, _game.Board, _game.Values);
        _game.DefaultCardMode = fileReader.ReadBoolean();
        LoadCardBase(fileReader, _game.Cards, _game.Players);
        foreach (IPlayer player in _game.Players)
            player.FindCardSet();
        // now that CardSets are mapped, Players' hands need to be tested for matching sets
        LoadStateMachine(fileReader);

        _game.Regulator.Initialize(_game, LoadRegulatorValues(fileReader));
    }
#pragma warning disable CA1822 // To maintain consistency with the rest of the deserialization methods, we ignore the recommendation to make this method static.
    /// <summary>
    /// Loads <see cref="IBoard"/> values from a file.
    /// </summary>
    /// <param name="fileReader">The <see cref="BinaryReader"/> for deserialization.</param>
    /// <param name="board">The <see cref="IBoard"/> to inject with loaded values.</param>
    public void LoadBoardState(BinaryReader fileReader, IBoard board)
    {
        for (int i = 0; i < board!.Geography.NumTerritories; i++)
            board.Armies[(TerrID)i] = fileReader.ReadInt32();

        for (int i = 0; i < board.Geography.NumTerritories; i++) {
            int owner = fileReader.ReadInt32();
            board.TerritoryOwner[(TerrID)i] = owner;
        }

        for (int i = 0; i < board.Geography.NumContinents; i++)
            board.ContinentOwner[(ContID)i] = fileReader.ReadInt32();
    }
#pragma warning restore CA1822
    /// <summary>
    /// Loads a series of <see cref="IPlayer"/> values from a file.
    /// </summary>
    /// <param name="fileReader">The <see cref="BinaryReader"/> for deserialization.</param>
    /// <param name="players">The <see cref="List{T}"/> of <see cref="IPlayer"/>s to initializes with loaded values.</param>
    /// <param name="board">A previously loaded <see cref="IBoard"/>.</param>
    /// <param name="values">A previously loaded <see cref="IRuleValues"/>.</param>
    public void LoadPlayerStates(BinaryReader fileReader, List<IPlayer> players, IBoard board, IRuleValues values)
    {
        int numPlayers = fileReader.ReadInt32();

        if (numPlayers > 1) {
            players.Clear();
            for (int i = 0; i < numPlayers; i++) {
                players.Add(new Player(
                    fileReader.ReadString(),
                    i,
                    numPlayers,
                    values!,
                    board!,
                    _logger));

                players[i].ArmyPool = fileReader.ReadInt32();
                players[i].ContinentBonus = fileReader.ReadInt32();
                int numTerritories = fileReader.ReadInt32();
                for (int j = 0; j < numTerritories; j++)
                    players[i].AddTerritory((TerrID)fileReader.ReadInt32());
                List<ICard> hand = [];
                LoadCardList(hand, fileReader);
                foreach (var card in hand)
                    players[i].AddCard(card);
            }
        }
    }
    /// <summary>
    /// Loads a card base from a file.
    /// </summary>
    /// <param name="fileReader">The <see cref="BinaryReader"/> for deserialization.</param>
    /// <param name="cardBase">The <see cref="CardBase"/> to initialize with loaded values.</param>
    /// <param name="players">A previously loaded <see cref="List{T}"/> of <see cref="IPlayer"/>s.</param>
    public void LoadCardBase(BinaryReader fileReader, CardBase cardBase, List<IPlayer> players)
    {
        int numSets = fileReader.ReadInt32();
        List<ICardSet>? loadedSets = LoadCardSets(fileReader, numSets);
        if (loadedSets == null) {
            _logger.LogError("LoadCardSets failed to return ICardSets. LoadCardBase execution aborted.");
            return;
        }

        List<ICard> library = [];
        LoadCardList(library, fileReader);
        List<ICard> discard = [];
        LoadCardList(discard, fileReader);

        cardBase ??= new(_logger);
        cardBase.Initialize(loadedSets, library, discard, players);
    }
    List<ICardSet>? LoadCardSets(BinaryReader fileReader, int numSets)
    {
        List<string> setNames = [];
        for (int i = 0; i < numSets; i++)
            setNames.Add(fileReader.ReadString());

        List<ICardSet>? loadedSets = [];
        foreach (string name in setNames) {
            Type? setType = _registry[name];
            if (setType == null) {
                _logger.LogWarning("A request for a Type registered to CardSet '{Name}' failed. The set will not be loaded. Ensure that the Registry is properly initialized.", name);
                continue;
            }
            if (setType.GetInterface(nameof(ICardSet)) == null) {
                _logger.LogWarning("{Type} was registered to {Name} as a CardSet, but does not implement {InterfaceName}. The set will not be loaded.", setType, name, nameof(ICardSet));
                continue;
            }
            var setInstance = (ICardSet?)Activator.CreateInstance(setType);
            if (setInstance == null) {
                _logger.LogError("An instance of {Type} failed to activate (it could not be cast to 'ICardSet'). The set will not be loaded.", setType);
                continue;
            }

            loadedSets.Add(setInstance);
        }
        return loadedSets;
    }
    void LoadCardList(List<ICard> oldList, BinaryReader fileReader)
    {
        oldList.Clear();
        int libraryCount = fileReader.ReadInt32();
        for (int i = 0; i < libraryCount; i++) {
            string cardTypeName = fileReader.ReadString();
            var cardType = _registry[cardTypeName] ?? throw new KeyNotFoundException($"{_registry} did not return a value for {cardTypeName}.");
            if (cardType.GetInterface(nameof(ICard)) == null)
                throw new ArgumentException($"{cardType} does not implement the necessary interface, {nameof(ICard)}.");

            if (Activator.CreateInstance(cardType, null) is not ICard card)
                throw new InvalidCastException($"Could not cast an instance of {cardType} to {nameof(ICard)}. Likely, {cardType} does not implement the interface.");

            int numProperties = fileReader.ReadInt32();
            bool initCard = true;
            for (int j = 0; j < numProperties; j++) {
                string propName = fileReader.ReadString();
                PropertyInfo? propInfo = cardType.GetProperty(propName) ?? throw new ArgumentException($"Type {cardType} does not contain a Property named {propName}.");
                int numValues = fileReader.ReadInt32();
                if (!card.InitializePropertyFromBinary(fileReader, propName, numValues)) {
                    initCard = false;
                    _logger.LogError("{PropertyName} on {CardType} failed to initialize via {NameOfThisMethod}.", propName, cardType, nameof(card.InitializePropertyFromBinary));
                }
            }

            if (initCard)
                oldList.Add(card);
        }
    }
    /// <summary>
    /// Loads values from a file into a game's <see cref="StateMachine"/>. See <see cref="IGame.State"/> and <see cref="_game"/>.
    /// </summary>
    /// <param name="fileReader"></param>
    public void LoadStateMachine(BinaryReader fileReader)
    {
        _game.State ??= new(_game.Players.Count);
        _game.State.InitializePlayerStatusArray(fileReader.ReadByte());
        _game.State.PhaseStageTwo = fileReader.ReadBoolean();
        _game.State.CurrentPhase = (GamePhase)fileReader.ReadInt32();
        _game.State.PlayerTurn = fileReader.ReadInt32();
        _game.State.Round = fileReader.ReadInt32();
        _game.State.NumTrades = fileReader.ReadInt32();
    }
    /// <summary>
    /// Loads values from a file into a game's <see cref="Regulator"/>. See <see cref="IGame.Regulator"/> and <see cref="_game"/>.
    /// </summary>
    /// <param name="fileReader"></param>
    public object?[] LoadRegulatorValues(BinaryReader fileReader)
    {
        object?[] values = new object?[5];
        values[0] = fileReader.ReadInt32();
        values[1] = fileReader.ReadInt32();
        values[2] = fileReader.ReadInt32();
        List<ICard> cardList = [];
        LoadCardList(cardList, fileReader);
        if (cardList.Count == 1) {
            values[3] = 1;
            values[4] = cardList[0];
        }
        else if (cardList.Count == 0) {
            values[3] = 0;
            values[4] = null;
        }

        return values;
    }
}