using Hazard_Model.Core;
using Hazard_Model.Entities;

namespace Hazard_Share.Interfaces.Model;

/// <summary>
/// Wraps the Model of the current game for injection into and observation by <see cref="Hazard_Share.Interfaces.ViewModel.IMainVM"/>.
/// </summary>
public interface IGame : IBinarySerializable
{
    /// <summary>
    /// Fires when a player loses, carrying their <see cref="IPlayer.Number"/>.
    /// </summary>
    event EventHandler<int>? PlayerLost;
    /// <summary>
    /// Fires when a player wins, carrying their <see cref="IPlayer.Number"/>.
    /// </summary>
    event EventHandler<int>? PlayerWon;
    /// <summary>
    /// Gets the unique ID of this <see cref="IGame"/>.
    /// </summary>
    /// <value>
    /// A new <see cref="Guid"/> unique to this instance.
    /// </value>
    /// <remarks>
    /// Useful for save/load and game data storage (to be implemented later).
    /// </remarks>
    public Guid ID { get; set; }
    /// <summary>
    /// Gets or sets a flag indicating whether the game is set to default card mode.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the <see cref="IGame"/> has been set to allow only the default cards. Otherwise, if others are added (e.g. Mission Cards), <see langword="false"/>.
    /// </value>
    public bool DefaultCardMode { get; set; }
    /// <summary>
    /// Gets or sets the list of players in the game.
    /// </summary>
    /// <value>
    /// A list of <see cref="IPlayer"/>, one for each player in the game. After initialization, should have a count of 2-6.
    /// </value>
    List<IPlayer> Players { get; set; }
    /// <summary>
    /// Gets the game's state machine, tracking player count and status, turns, etc.
    /// </summary>
    /// <value>
    /// An instance of <see cref="StateMachine"/>, or <see langword="null"/> if the game has not been initialized.
    /// </value>
    StateMachine State { get; }
    /// <summary>
    /// Gets or sets an instance describing the Game board; stores both data and relations between Board objects.
    /// </summary>
    /// <value>
    /// An instance of <see cref="IBoard"/>.
    /// </value>
    IBoard Board { get; set; }
    /// <summary>
    /// Gets or sets the "card base", containing all <see cref="ICard"/>s, <see cref="ICardSet"/>s, <see cref="Deck"/>s, maps between them, and associated methods.
    /// </summary>
    /// <value>
    /// A <see cref="CardBase"/> instance.
    /// </value>
    CardBase Cards { get; set; }
    /// <summary>
    /// Gets a service which "regulates" interaction between the model and the players (interprets player actions according to the game state, then executes logic according to game rules).
    /// </summary>
    /// <value>
    /// An instance of <see cref="IBoard"/>.
    /// </value>
    //IRegulator Regulator { get; }
    /// <summary>
    /// Gets or sets a data object containing game-specific rules values, like continent bonuses or equations for bonus armies.
    /// </summary>
    /// <value>
    /// An instance of <see cref="IRuleValues"/>.
    /// </value>
    IRuleValues Values { get; set; }


    abstract void Initialize(string[] names, string? fileName, long? streamLoc);
    /// <summary>
    /// Save game state to a file.
    /// </summary>
    /// <param name="isNewFile">A boolean indicating whether the save file is new.</param>
    /// <param name="fileName">The name of the save file.</param>
    /// <param name="precedingData">Data from the View and/or ViewModel to be written to the save file first.</param>
    abstract Task Save(bool isNewFile, string fileName, string precedingData);
}