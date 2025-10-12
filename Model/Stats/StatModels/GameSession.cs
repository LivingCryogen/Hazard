using Microsoft.Extensions.Logging;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;

namespace Model.Stats.StatModels;
/// <summary>
/// Statistics model for a game session.
/// </summary>
/// <param name="logger">The logger provided by DI.</param>
/// <param name="loggerFactory">The logger factory provided by DI.</param>
public class GameSession(ILogger<GameSession> logger, ILoggerFactory loggerFactory) : IBinarySerializable
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly ILogger _logger = logger;

    /// <summary>
    /// A model for Attack action data.
    /// </summary>
    /// <param name="logger">The logger provided by DI or a factory.</param>
    public class AttackAction(ILogger<AttackAction> logger) : IBinarySerializable
    {
        private readonly ILogger _logger = logger;
        /// <summary>
        /// Gets or sets the id for this action (should be sequential/incremental).
        /// </summary>
        public int ActionId { get; set; }
        /// <summary>
        /// Gets or sets the source territory of the attack.
        /// </summary>
        public TerrID SourceTerritory { get; set; }
        /// <summary>
        /// Gets or sets the target territory of the attack.
        /// </summary>
        public TerrID TargetTerritory { get; set; }
        /// <summary>
        /// Gets or sets player ID of the attacker.
        /// </summary>
        /// <value>0-5</value>
        public int Attacker { get; set; }
        /// <summary>
        /// Gets or sets player ID of the defender.
        /// </summary>
        /// <value>0-5</value>
        public int Defender { get; set; }
        /// <summary>
        /// Gets or sets the number of armies the attacker had at the beginning of the attack.
        /// </summary>
        public int AttackerInitialArmies { get; set; }
        /// <summary>
        /// Gets or sets the number of armies the defender had at the beginning of the attack.
        /// </summary>
        public int DefenderInitialArmies { get; set; }
        /// <summary>
        /// Gets or sets the number of dice the attacker rolled.
        /// </summary>
        public int AttackerDice { get; set; }
        /// <summary>
        /// Gets or sets the number of dice the defender rolled.
        /// </summary>
        public int DefenderDice { get; set; }
        /// <summary>
        /// Gets or sets the number of units lost by the attacker.
        /// </summary>
        public int AttackerLoss { get; set; }
        /// <summary>
        /// Gets or sets the number of units lost by the defender.
        /// </summary>
        public int DefenderLoss { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the attacker was forced to retreat.
        /// </summary>
        public bool Retreated { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the target territory was conquered with this attack.
        /// </summary>
        public bool Conquered { get; set; }

        /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>
        public async Task<SerializedData[]> GetBinarySerials()
        {
            return await Task.Run(() =>
            {
                List<SerializedData> saveData = [];
                saveData.Add(new(typeof(TerrID), SourceTerritory));
                saveData.Add(new(typeof(TerrID), TargetTerritory));

                saveData.Add(new(typeof(int), Attacker));
                saveData.Add(new(typeof(int), Defender));
                saveData.Add(new(typeof(int), AttackerLoss));
                saveData.Add(new(typeof(int), DefenderLoss));
                saveData.Add(new(typeof(bool), Retreated));
                saveData.Add(new(typeof(bool), Conquered));
                return saveData.ToArray();
            });
        }
        /// <inheritdoc cref="IBinarySerializable.LoadFromBinary"/>/>
        public bool LoadFromBinary(BinaryReader reader)
        {
            bool loadComplete = true;
            try
            {
                SourceTerritory = (TerrID)BinarySerializer.ReadConvertible(reader, typeof(TerrID));
                TargetTerritory = (TerrID)BinarySerializer.ReadConvertible(reader, typeof(TerrID));
                Attacker = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                Defender = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                AttackerLoss = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                DefenderLoss = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                Retreated = (bool)BinarySerializer.ReadConvertible(reader, typeof(bool));
                Conquered = (bool)BinarySerializer.ReadConvertible(reader, typeof(bool));
            }
            catch (Exception ex)
            {
                _logger.LogError("An exception was thrown while loading {AttackAction}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
                loadComplete = false;
            }
            return loadComplete;
        }
    }

    /// <summary>
    /// A model for Move action data.
    /// </summary>
    /// <param name="logger">The logger provided by DI or a factory.</param>
    public class MoveAction(ILogger<MoveAction> logger) : IBinarySerializable
    {
        private readonly ILogger _logger = logger;
        /// <summary>
        /// Gets or sets the id for this action (should be sequential/incremental).
        /// </summary>
        public int ActionId { get; set; }
        /// <summary>
        /// Gets or sets the source territory of the move.
        /// </summary>
        public TerrID SourceTerritory{ get; set; }
        /// <summary>
        /// Gets or sets the target territory of the move.
        /// </summary>
        public TerrID TargetTerritory { get; set; }
        /// <summary>
        /// Gets or sets the identifier of the moving player.
        /// </summary>
        public int Player { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the maximum possible number of armies moved.
        /// </summary>
        public bool MaxAdvanced { get; set; }

        /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>
        public async Task<SerializedData[]> GetBinarySerials()
        {
            return await Task.Run(() =>
            {
                List<SerializedData> saveData = [];
                saveData.Add(new(typeof(TerrID), SourceTerritory));
                saveData.Add(new(typeof(TerrID), TargetTerritory));
                saveData.Add(new(typeof(int), Player));
                saveData.Add(new(typeof(bool), MaxAdvanced));
                return saveData.ToArray();
            });
        }
        /// <inheritdoc cref="IBinarySerializable.LoadFromBinary"/>/>
        public bool LoadFromBinary(BinaryReader reader)
        {
            bool loadComplete = true;
            try
            {
                SourceTerritory = (TerrID)BinarySerializer.ReadConvertible(reader, typeof(TerrID));
                TargetTerritory = (TerrID)BinarySerializer.ReadConvertible(reader, typeof(TerrID));
                Player = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                MaxAdvanced = (bool)BinarySerializer.ReadConvertible(reader, typeof(bool));
            }
            catch (Exception ex)
            {
                _logger.LogError("An exception was thrown while loading {MoveAction}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
                loadComplete = false;
            }
            return loadComplete;
        }
    }
    /// <summary>
    /// A model for Trade action data.
    /// </summary>
    /// <param name="logger">The logger provided by DI or a factory.</param>
    public class TradeAction(ILogger<TradeAction> logger) : IBinarySerializable
    {
        private readonly ILogger _logger = logger;
        /// <summary>
        /// Gets or sets the id for this action (should be sequential/incremental).
        /// </summary>
        public int ActionId { get; set; }
        /// <summary>
        /// Gets or sets the array of target territory identifiers associated with the card.
        /// </summary>
        public TerrID[] CardTargetTerritories { get; set; } = [];
        /// <summary>
        /// Gets or sets the base trade value (additional armies) of the trade-in.
        /// </summary>
        public int TradeValue { get; set; }
        /// <summary>
        /// Gets or sets the bonus gained for controlling a target territory at the time of trade-in.
        /// </summary>
        /// <value>2 if an occupation bonus was gained; otherwise, 0.</value>
        public int OccupiedBonus { get; set; }
        /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>
        public async Task<SerializedData[]> GetBinarySerials()
        {
            return await Task.Run(() =>
            {
                List<SerializedData> saveData = [];

                List<IConvertible> convertTargetTerritorys = [];
                foreach (var target in CardTargetTerritories)
                    convertTargetTerritorys.Add(target);
                saveData.Add(new(typeof(int), convertTargetTerritorys.Count));
                saveData.Add(new(typeof(TerrID), [.. convertTargetTerritorys]));

                saveData.Add(new(typeof(int), TradeValue));
                saveData.Add(new(typeof(int), OccupiedBonus));
                return saveData.ToArray();
            });
        }
        /// <inheritdoc cref="IBinarySerializable.LoadFromBinary"/>/>
        public bool LoadFromBinary(BinaryReader reader)
        {
            bool loadComplete = true;
            try
            {
                int numCardTargetTerritorys = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                CardTargetTerritories = (TerrID[])BinarySerializer.ReadConvertibles(reader, typeof(TerrID), numCardTargetTerritorys);
                TradeValue = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                OccupiedBonus = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            }
            catch (Exception ex)
            {
                _logger.LogError("An exception was thrown while loading {MoveAction}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
                loadComplete = false;
            }
            return loadComplete;
        }
    }
    /// <summary>
    /// Gets or sets the unique identifier of the game session.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets the unique identifier of the installation where the game session took place.
    /// </summary>
    public Guid InstallId { get; set; }
    /// <summary>
    /// Gets the number of tracked actions.
    /// </summary>
    /// <remarks>
    /// Allows <see cref="StatRepo"/> to easily determine which is the most up-to-date game file for a given game ID.
    /// </remarks>
    public int NumActions { get => Attacks.Count + Moves.Count + TradeIns.Count; }
    /// <summary>
    /// Gets or sets the start time of the game session.
    /// </summary>
    public DateTime StartTime { get; set; }
    /// <summary>
    /// Gets or sets the end time of the game session, if applicable.
    /// </summary>
    /// <value>
    /// <see langword="null"/> if the game session is still ongoing; otherwise, the end time of the session.
    /// </value>
    public DateTime? EndTime { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the winning player, if applicable.
    /// </summary>
    /// <value>
    /// <see langword="null"/> if there is no winner (e.g., the game is still ongoing); otherwise, the player ID of the winner."
    /// </value>
    public int? Winner { get; set; }
    /// <summary>
    /// Gets or sets the list of attack actions recorded during the game session.
    /// </summary>
    public List<AttackAction> Attacks { get; private set; } = [];
    /// <summary>
    /// Gets or sets the list of move actions recorded during the game session.
    /// </summary>
    public List<MoveAction> Moves { get; private set; } = [];
    /// <summary>
    /// Gets or sets the list of trade-in actions recorded during the game session.
    /// </summary>
    public List<TradeAction> TradeIns { get; private set; } = [];
    /// <summary>
    /// Gets or sets a map of player numbers to their names in this game.
    /// </summary>
    public Dictionary<int, string> PlayerNumsAndNames { get; set; } = [];

    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try
        {
            Id = Guid.Parse((string)BinarySerializer.ReadConvertible(reader, typeof(string)));
            InstallId = Guid.Parse((string)BinarySerializer.ReadConvertible(reader, typeof(string)));
            StartTime = DateTime.Parse((string)BinarySerializer.ReadConvertible(reader, typeof(string)));

            bool hasEndTime = (int)BinarySerializer.ReadConvertible(reader, typeof(int)) == 1;
            if (hasEndTime)
                EndTime = DateTime.Parse((string)BinarySerializer.ReadConvertible(reader, typeof(string)));

            bool hasWinner = (int)BinarySerializer.ReadConvertible(reader, typeof(int)) == 1;
            if (hasWinner)
                Winner = (int)BinarySerializer.ReadConvertible(reader, typeof(int));

            int numAttacks = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            for (int i = 0; i < numAttacks; i++)
            {
                var readAction = new AttackAction(_loggerFactory.CreateLogger<AttackAction>());
                readAction.LoadFromBinary(reader);
                Attacks.Add(readAction);
            }

            int numMoves = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            for (int i = 0; i < numMoves; i++)
            {
                var readMove = new MoveAction(_loggerFactory.CreateLogger<MoveAction>());
                readMove.LoadFromBinary(reader);
                Moves.Add(readMove);
            }

            int numTrades = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            for (int i = 0; i < numTrades; i++)
            {
                var readTrade = new TradeAction(_loggerFactory.CreateLogger<TradeAction>());
                readTrade.LoadFromBinary(reader);
                TradeIns.Add(readTrade);
            }

            List<(int, string)> ReadPlayerNumsNames = [];
            int numMappedPlayers = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            for (int i = 0; i < numMappedPlayers; i++)
            {
                int playerNum = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                string playerName = (string)BinarySerializer.ReadConvertible(reader, typeof(string));
                ReadPlayerNumsNames.Add((playerNum, playerName));
            }
            PlayerNumsAndNames = ReadPlayerNumsNames.ToDictionary<int, string>();
        }
        catch (Exception ex)
        {
            _logger.LogError("An exception was thrown while loading {GameSession}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
    /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>/>
    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(async () =>
        {
            List<SerializedData> saveData = [];
            saveData.Add(new(typeof(string), Id.ToString()));
            saveData.Add(new(typeof(string), InstallId.ToString()));
            saveData.Add(new(typeof(string), StartTime.ToString()));

            if (EndTime is DateTime endTime)
            {
                saveData.Add(new(typeof(int), 1));
                saveData.Add(new(typeof(string), endTime.ToString()));
            }
            else
                saveData.Add(new(typeof(int), 0));

            if (Winner is int winner)
            {
                saveData.Add(new(typeof(int), 1));
                saveData.Add(new(typeof(int), winner));
            }
            else
                saveData.Add(new(typeof(int), 0));

            var attackSaveTasks = Attacks.Select(a => a.GetBinarySerials());
            var moveSaveTasks = Moves.Select(m => m.GetBinarySerials());
            var tradeSaveTasks = TradeIns.Select(t => t.GetBinarySerials());

            var saveTasks = new[]
            {
                Task.WhenAll(attackSaveTasks),
                Task.WhenAll(moveSaveTasks),
                Task.WhenAll(tradeSaveTasks),
            };

            var innerSaveData = await Task.WhenAll(saveTasks);

            int numAttacks = Attacks.Count;
            saveData.Add(new(typeof(int), numAttacks));
            saveData.AddRange(innerSaveData[0].SelectMany(a => a));

            int numMoves = Moves.Count;
            saveData.Add(new(typeof(int), numMoves));
            saveData.AddRange(innerSaveData[1].SelectMany(m => m));

            int numTrades = TradeIns.Count;
            saveData.Add(new(typeof(int), numTrades));
            saveData.AddRange(innerSaveData[2].SelectMany(t => t));

            int numMappedPlayers = PlayerNumsAndNames.Count;
            saveData.Add(new(typeof(int), numMappedPlayers));
            for(int i = 0; i < numMappedPlayers; i++)
            {
                saveData.Add(new(typeof(int), PlayerNumsAndNames.Keys.ToArray()[i]));
                saveData.Add(new(typeof(string), PlayerNumsAndNames[i]));
            }

            return saveData.ToArray();
        });
    }
}

