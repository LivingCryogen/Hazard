using Hazard_Model.Entities.Cards;
using Hazard_Share.Enums;
using Hazard_Share.Interfaces.Model;
using Hazard_Share.Services.Serializer;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Xml.Linq;

namespace Hazard_Model.Core;
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
    private readonly int _numPlayers = 0;
    private BitArray _isActivePlayer;
    private int _numTrades = 0;

    /// <summary>
    /// Indicates that a <see cref="StateMachine"/> public property has changed value.
    /// </summary>
    /// <remarks>
    /// Both <see cref="Regulator"/> and <see cref="Hazard_Share.Interfaces.ViewModel.IMainVM"/> subscribe to this event.
    /// </remarks>
    public EventHandler<string>? StateChanged;

    /// <summary>
    /// Constructs the machine based on the number of players in the game.
    /// </summary>
    /// <param name="numPlayers">The number of players.</param>
    /// <exception cref="StateMachine(int)">Thrown when the number of players given is not 2-6.</exception>
    public StateMachine(int numPlayers, ILogger<StateMachine> logger)
    {
        _numPlayers = numPlayers;
        _logger = logger;

        if (_numPlayers == 2)
            CurrentPhase = GamePhase.TwoPlayerSetup;
        else if (_numPlayers > 2 && _numPlayers < 7)
            CurrentPhase = GamePhase.DefaultSetup;
        else
            throw new ArgumentOutOfRangeException(nameof(numPlayers));

        _isActivePlayer = new(_numPlayers);
        _isActivePlayer.SetAll(true);
    }

    #region Properties
    /// <summary>
    /// Gets what phase the game is currently in, or sets the phase and invokes <see cref="StateChanged"/>.
    /// </summary>
    /// <value>
    /// A <see cref="GamePhase"/> value representing the phase the game is in.
    /// </value>
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
    /// <example>E.g.: In the first stage of <see cref="GamePhase.Attack"/>, the application must accept player input of the attack source.
    /// In the second stage, it must use this selection together with a second input: the attack target.</example>
    /// </summary>
    /// <value>True if the <see cref="CurrentPhase"/> is a two-stage <see cref="GamePhase"/> and is in its second stage. Otherwise, false.</value>  
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
    /// Gets the number of rounds the game has entered. Or, sets this value and invokes <see cref="StateChanged"/>.
    /// </summary>
    /// <remarks>A round is completed once <see cref="PlayerTurn"/> increases beyond <see cref="_numPlayers"/>. The 0th Round is the setup round.</remarks>
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
    #endregion

    #region Methods
    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(() =>
        {
            SerializedData[] saveData = [
                new(typeof(byte), [BitArrayToByte(_isActivePlayer)]),
                new(typeof(bool), [_phaseStageTwo]),
                new(typeof(GamePhase), [_currentPhase]),
                new(typeof(int), [_playerTurn]),
                new(typeof(int), [_round]),
                new(typeof(int), [_numTrades]),
                new(typeof(int), [Winner])
            ];
            return saveData;
        });
    }
    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try {
            _isActivePlayer = new((byte)BinarySerializer.ReadConvertible(reader, typeof(byte)));
            _phaseStageTwo = (bool)BinarySerializer.ReadConvertible(reader, typeof(bool));
            _currentPhase = (GamePhase)BinarySerializer.ReadConvertible(reader, typeof(GamePhase));
            _playerTurn = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            _round = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            _numTrades = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            Winner = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
        } catch (Exception ex) {
            _logger.LogError("An exception was thrown while loading {Player}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
    /// <summary>
    /// End the current round and perform end of round actions.
    /// </summary>
    /// <exception cref="Exception"></exception>
    public void IncrementRound()
    {
        if (PhaseStageTwo)
            PhaseStageTwo = false;

        int firstActive = NextActivePlayer(0);
        if (firstActive != -1)
            PlayerTurn = firstActive;
        else
            throw new Exception("No Active Player Remaining!");

        CurrentPhase = GamePhase.Place;
        Round++;
    }
    /// <summary>
    /// End the current <see cref="PlayerTurn"/> and perform end of turn actions.
    /// </summary>
    /// <exception cref="Exception"></exception>
    public void IncrementPlayerTurn()
    {
        if (CurrentPhase.Equals(GamePhase.DefaultSetup) || CurrentPhase.Equals(GamePhase.TwoPlayerSetup)) {
            if (PlayerTurn >= _numPlayers)
                PlayerTurn = 0;
            else {
                int firstActive = NextActivePlayer();
                if (firstActive != -1)
                    PlayerTurn = firstActive;
                else
                    throw new Exception("No Active Player Remaining!");
            }
        }
        else if (PlayerTurn >= _numPlayers) {
            IncrementRound();
        }
        else {
            int firstActive = NextActivePlayer();
            if (firstActive != -1)
                PlayerTurn = firstActive;
            else
                throw new Exception("No Active Player Remaining!");
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
    /// <seealso><para>See <see cref="NextActivePlayer()"/>, <see cref="NextActivePlayer(int)"/>, and <see cref="_isActivePlayer"/>.</para></seealso>
    /// </remarks>
    /// <param name="player"></param>
    public void DisablePlayer(int player)
    {
        if (player >= 0 && player < _numPlayers)
            _isActivePlayer[player] = false;
    }
    private int NextActivePlayer() // begins checking at PlayerTurn + 1, looping to beginning if at the end of the Player list
    {
        int start;
        if (PlayerTurn == _numPlayers - 1)
            start = 0;
        else
            start = PlayerTurn + 1;

        int index = start;
        do {
            if (_isActivePlayer[index])
                return index;
            else
                index++;

            if (index > _numPlayers - 1)
                index = 0;

        } while (index != start);

        return -1;
    }
    private int NextActivePlayer(int start) // begins checking at a specified player number, *inclusive*
    {
        if (start >= 0 && start < _numPlayers) {
            int index = start;
            do {
                if (_isActivePlayer[index])
                    return index;
                else
                    index++;

                if (index > _numPlayers - 1)
                    index = 0;

            } while (index != start);
            return -1;
        }
        else throw new ArgumentOutOfRangeException(nameof(start));
    }
    /// <summary>
    /// Initializes <see cref="_isActivePlayer"/> from a saved game. 
    /// </summary>
    /// <param name="data">A <see cref="byte"/> retrieved from <see cref="Hazard_Model.DataAccess.BinarySerializer.LoadStateMachine(BinaryReader)"/>.</param>
    public void InitializePlayerStatusArray(byte data)
    {
        byte[] dataArray = [data];
        _isActivePlayer = new(dataArray);
    }
    /// <summary>
    /// Converts a <see cref="BitArray"/> of length 8 or less into a <see cref="byte"/> for binary serialization.
    /// </summary>
    /// <param name="bitArray"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
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
    #endregion
}
