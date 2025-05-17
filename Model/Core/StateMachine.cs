using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;
using System.Collections;

namespace Model.Core;
/// <summary>
/// The game's state machine.
/// </summary>
public class StateMachine : IBinarySerializable
{
    private readonly ILogger<StateMachine> _logger;
    private GamePhase _currentPhase = GamePhase.Null;
    private bool _phaseStageTwo = false;
    private int _playerTurn = 0;
    private int _round = 0;
    private int _numTrades = 0;

    /// <summary>
    /// Indicates that a <see cref="StateMachine"/> public property has changed value.
    /// </summary>
    /// <remarks>
    /// Both <see cref="Regulator"/> and <see cref="Shared.Interfaces.ViewModel.IMainVM"/> subscribe to this event.
    /// </remarks>
    public EventHandler<string>? StateChanged;

    /// <summary>
    /// Constructs the machine based on the number of players in the game.
    /// </summary>
    /// <param name="numPlayers">The number of players.</param>
    /// <param name="logger">Provided by an <see cref="ILoggerFactory"/> during <see cref="Game"/> construction.</param>
    public StateMachine(int numPlayers, ILogger<StateMachine> logger)
    {
        NumPlayers = numPlayers;
        _logger = logger;

        CurrentPhase = NumPlayers switch {
            2 => GamePhase.TwoPlayerSetup,
            int n when n > 2 && n < 7 => GamePhase.DefaultSetup,
            _ => GamePhase.Null,
        };

        IsActivePlayer = new(NumPlayers);
        IsActivePlayer.SetAll(true);
    }

    /// <summary>
    /// Gets or sets the <see cref="int">number</see> of <see cref="IPlayer"/> in the parent <see cref="Game"/>.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> from 2-6.
    /// </value>
    public int NumPlayers { get; private set; }
    /// <summary>
    /// Gets or sets an array of flags indicating which <see cref="IPlayer"/>s are active.
    /// </summary>
    /// <value>
    /// A <see cref="BitArray"/> of one <see cref="byte"/>. Each index of the array corresponds to a <see cref="IPlayer.Number"/>,<br/>
    /// with a <see langword="true"/> (<see cref="int">1</see>) value indicating a player is active, and <see langword="false"/> (<see cref="int">0</see>) inactive.
    /// </value>
    public BitArray IsActivePlayer { get; private set; }
    /// <summary>
    /// Gets what phase the game is currently in, or sets the phase and invokes <see cref="StateChanged"/>.
    /// </summary>
    public GamePhase CurrentPhase {
        get { return _currentPhase; }
        set {
            if (!value.Equals(_currentPhase)) {
                _currentPhase = value;
                StateChanged?.Invoke(this, new(nameof(CurrentPhase)));
            }
        }
    }
    /// <summary>
    /// Gets a value indicating whether a two-part phase is in the first or second stage. Or, sets this value and invokes <see cref="StateChanged"/>.
    /// <para><example>E.g.: In the first stage of <see cref="GamePhase.Attack"/>, the model must accept input of the attack source.<br/>
    /// In the second stage, it must use this selection together with a second input: the attack target.</example></para>
    /// </summary>
    /// <value><see langword="true"/> if the <see cref="CurrentPhase"/> is a two-stage <see cref="GamePhase"/> and is in its second stage. Otherwise, <see langword="false"/>.</value>  
    public bool PhaseStageTwo {
        get { return _phaseStageTwo; }
        set {
            if (!value.Equals(_phaseStageTwo)) {
                _phaseStageTwo = value;
                StateChanged?.Invoke(this, new(nameof(PhaseStageTwo)));
            }
        }
    }
    /// <summary>
    /// Gets the number of the player whose turn it is. Or, sets this value and invokes <see cref="StateChanged"/>.
    /// </summary>
    /// <value>
    /// An integer assignation for a player, from 0-5. 
    /// </value>
    public int PlayerTurn {
        get { return _playerTurn; }
        set {
            if (!value.Equals(_playerTurn)) {
                _playerTurn = value;
                StateChanged?.Invoke(this, new(nameof(PlayerTurn)));
            }
        }
    }
    /// <summary>
    /// Gets the number of rounds the game has entered so far. Or, sets this value and invokes <see cref="StateChanged"/>.
    /// </summary>
    /// <remarks>A round is completed once <see cref="PlayerTurn"/> increases beyond <see cref="NumPlayers"/>. The 0th Round is the setup round.</remarks>
    /// <value>
    /// An integer representing the number of times the turn has passed to each player.
    /// </value>
    public int Round {
        get { return _round; }
        set {
            if (!value.Equals(_round)) {
                _round = value;
                StateChanged?.Invoke(this, new(nameof(Round)));
            }
        }
    }
    /// <summary>
    /// Gets the number of times a set of cards has been traded in for bonus armies during the current game. Or, sets this value and invokes <see cref="StateChanged"/>.
    /// </summary>
    /// <value>
    /// An integer of 0 or more that is incremented each time a player trades in a set of cards.
    /// </value>
    public int NumTrades {
        get { return _numTrades; }
        set {
            if (!value.Equals(_numTrades)) {
                _numTrades = value;
                StateChanged?.Invoke(this, new(nameof(NumTrades)));
            }
        }
    }
    /// <summary>
    /// Gets or sets a value designating a player as the game winner.
    /// </summary>
    /// <value>
    /// An integer from 0-5.
    /// </value>
    public int Winner { get; set; }

    /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>
    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(() =>
        {
            SerializedData[] saveData = [
                new(typeof(int), [NumPlayers]),
                new(typeof(byte), [BitArrayToByte(IsActivePlayer)]),
                new(typeof(bool), [_phaseStageTwo]),
                new(typeof(GamePhase), [_currentPhase]),
                new(typeof(int), [_playerTurn]),
                new(typeof(int), [_round]),
                new(typeof(int), [_numTrades]),
            ];
            return saveData;
        });
    }
    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try {
            NumPlayers = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            byte activePlayerByte = (byte)BinarySerializer.ReadConvertible(reader, typeof(byte));
            IsActivePlayer = new(new byte[] { activePlayerByte }); // Reverse of "BitArrayToByte()" method
            _phaseStageTwo = (bool)BinarySerializer.ReadConvertible(reader, typeof(bool));
            _currentPhase = (GamePhase)BinarySerializer.ReadConvertible(reader, typeof(GamePhase));
            _playerTurn = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            _round = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            _numTrades = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
        } catch (Exception ex) {
            _logger.LogError("An exception was thrown while loading {Player}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
    /// <summary>
    /// End the current round and perform end of round actions.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if there is an attempt to calculate the next active player when all players are inactive.</exception>
    public void IncrementRound()
    {
        if (PhaseStageTwo)
            PhaseStageTwo = false;

        int firstActive = NextActivePlayer(0);
        if (firstActive != -1)
            PlayerTurn = firstActive;
        else
            throw new InvalidOperationException("No Active Player Remaining!");

        CurrentPhase = GamePhase.Place;
        Round++;
    }
    /// <summary>
    /// End the current <see cref="PlayerTurn"/> and perform end of turn actions.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if there is an attempt to calculate the next active player when all players are inactive.</exception>
    public void IncrementPlayerTurn()
    {
        if (CurrentPhase == GamePhase.DefaultSetup || CurrentPhase == GamePhase.TwoPlayerSetup) {
            if (PlayerTurn >= NumPlayers)
                PlayerTurn = 0;
            else {
                int firstActive = NextActivePlayer();
                if (firstActive != -1)
                    PlayerTurn = firstActive;
                else
                    throw new InvalidOperationException("No Active Player Remaining!");
            }
        }
        else if (PlayerTurn >= NumPlayers) {
            IncrementRound();
        }
        else {
            int firstActive = NextActivePlayer();
            if (firstActive != -1)
                PlayerTurn = firstActive;
            else
                throw new InvalidOperationException("No Active Player Remaining!");
            CurrentPhase = GamePhase.Place;
        }
    }
    /// <summary>
    /// End the current <see cref="GamePhase"/> and perform end of phase actions.
    /// </summary>
    public void IncrementPhase()
    {
        PhaseStageTwo = false;
        int intPhase = (int)CurrentPhase;
        if (intPhase > -1 && intPhase < 3) // DefaultSetup (0), Place (1), Attack (2), Move (3)
        {
            intPhase++;
            CurrentPhase = (GamePhase)intPhase;
        }
        else if (intPhase == 3) {
            IncrementPlayerTurn();
        }
        else if (intPhase == -1) // TwoPlayerSetup (-1)
        {
            intPhase += 2;
            CurrentPhase = (GamePhase)intPhase;
        }
    }
    /// <summary>
    /// Increase <see cref="NumTrades"/> by a fixed amount.
    /// </summary>
    /// <param name="increment">The amount of increase.</param>
    public void IncrementNumTrades(int increment)
    {
        NumTrades += increment;
    }
    /// <summary>
    /// Disable a player.
    /// </summary>
    /// <remarks>
    /// <para>Typically this is reserved for when a player has been defeated.<br/>
    /// Disable works by "skipping" disabled player numbers when incrementing <see cref="PlayerTurn"/>s and <see cref="Round"/>s.</para>
    /// <seealso><para>See <see cref="NextActivePlayer()"/>, <see cref="NextActivePlayer(int)"/>, and <see cref="IsActivePlayer"/>.</para></seealso>
    /// </remarks>
    /// <param name="player"></param>
    public void DisablePlayer(int player)
    {
        if (player >= 0 && player < NumPlayers)
            IsActivePlayer[player] = false;
    }
    private int NextActivePlayer() // begins checking at PlayerTurn + 1, looping to beginning if at the end of the Player list
    {
        int start;
        // If we're at the last player, start at the beginning. Otherwise, start at the next player.
        if (PlayerTurn == NumPlayers - 1)
            start = 0;
        else
            start = PlayerTurn + 1;

        // Loop until we find an active player or we've checked all of them.
        int index = start;
        do {
            if (IsActivePlayer[index])
                return index;
            else
                index++;

            if (index > NumPlayers - 1)
                index = 0;

        } while (index != start);

        return -1;
    }
    private int NextActivePlayer(int start) // begins checking at a specified player number, *inclusive*
    {
        if (start >= 0 && start < NumPlayers) {
            int index = start;
            do {
                if (IsActivePlayer[index])
                    return index;
                else
                    index++;

                if (index > NumPlayers - 1)
                    index = 0;

            } while (index != start);
            return -1;
        }
        else throw new ArgumentOutOfRangeException(nameof(start));
    }
    /// <summary>
    /// Initializes <see cref="IsActivePlayer"/> from a saved game. 
    /// </summary>
    /// <param name="data">Typically, data from <see cref="BinarySerializer.ReadConvertible(BinaryReader, Type)"/>.</param>
    public void InitializePlayerStatusArray(byte data)
    {
        byte[] dataArray = [data];
        IsActivePlayer = new(dataArray);
    }
    /// <summary>
    /// Converts a <see cref="BitArray"/> of length 8 or less into a <see cref="byte"/> for binary serialization.
    /// </summary>
    /// <param name="bitArray">The array to convert.</param>
    /// <returns>The converted byte.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="bitArray"/> has a length greater than 8.</exception>
    private static byte BitArrayToByte(BitArray bitArray)
    {
        byte[] converted = new byte[1];
        if (bitArray.Count > 8)
            throw new ArgumentOutOfRangeException(nameof(bitArray));
        else {
            bitArray.CopyTo(converted, 0);
            return converted[0];
        }
    }
}
