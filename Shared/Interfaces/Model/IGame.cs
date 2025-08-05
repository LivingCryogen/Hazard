using Model.Core;
using Model.Entities;

namespace Shared.Interfaces.Model;

/// <summary>
/// Encapsulates the current game for injection into <see cref="ViewModel.IMainVM"/>.
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
    /// Gets the facade of the DAL.
    /// </summary>
    public IAssetFetcher AssetFetcher { get; }
    /// <summary>
    /// Gets the unique ID of the game.
    /// </summary>
    public Guid ID { get; }
    /// <summary>
    /// Gets a flag indicating whether the game is set to default card mode.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the <see cref="IGame"/> has been set to allow only the default cards. Otherwise, if others are added (e.g. Mission Cards), <see langword="false"/>.
    /// </value>
    public bool DefaultCardMode { get; }
    /// <summary>
    /// Gets a data object containing game-specific rules values, like continent bonuses or equations for bonus armies.
    /// </summary>
    IRuleValues Values { get; }
    /// <summary>
    /// Gets an instance describing the Game board; stores both data and relations between Board objects.
    /// </summary>
    IBoard Board { get; }
    /// <summary>
    /// Gets the game's state machine, which tracks player count and status, turns, etc.
    /// </summary>
    StateMachine State { get; }
    /// <summary>
    /// Gets the "card base", containing all <see cref="ICard"/>s, <see cref="ICardSet"/>s, and <see cref="Deck"/>s.
    /// </summary>
    CardBase Cards { get; }
    /// <summary>
    /// Gets the list of players in the game.
    /// </summary>
    /// <value>
    /// After initialization and/or loading, should have a count of 2-6.
    /// </value>
    List<IPlayer> Players { get; }
    /// <summary>
    /// Gets the Stat Tracker for this game.
    /// </summary>
    public IStatTracker StatTracker { get; }
    /// <summary>
    /// Gets or sets the path to the last used save file path for this game.
    /// </summary>
    public string? SavePath { get; set; }

    /// <summary>
    /// Updates player names.
    /// </summary>
    /// <remarks>
    /// Useful after loading a game from a file.
    /// </remarks>
    /// <param name="names">Player names in ascending player number order.</param>
    abstract void UpdatePlayerNames(string[] names);
    /// <summary>
    /// Save to a file.
    /// </summary>
    /// <param name="isNewFile">Indicates whether the save file is new.</param>
    /// <param name="fileName">The name of the save file.</param>
    public Task Save(bool isNewFile, string fileName);
}