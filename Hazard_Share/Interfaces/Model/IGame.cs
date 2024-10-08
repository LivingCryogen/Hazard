﻿using Hazard_Model.Core;
using Hazard_Model.Entities;

namespace Hazard_Share.Interfaces.Model;

/// <summary>
/// Wraps the Model of the current game for injection into and observation by <see cref="Hazard_Share.Interfaces.ViewModel.IMainVM"/>.
/// </summary>
public interface IGame
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
    /// A new <see cref="Guid"/> unique to this instance, if initialized; otherwise, <see langword="null"/>.
    /// </value>
    /// <remarks>
    /// Useful for save/load and game data storage (to be implemented later).
    /// </remarks>
    public Guid? ID { get; set; }
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
    /// Gets or sets the game's state machine, tracking player count and status, turns, etc.
    /// </summary>
    /// <value>
    /// An instance of <see cref="StateMachine"/>, or <see langword="null"/> if the game has not been initialized.
    /// </value>
    StateMachine? State { get; set; }
    /// <summary>
    /// Gets or sets an instance describing the Game board; stores both data and relations between Board objects.
    /// </summary>
    /// <value>
    /// An instance of <see cref="IBoard"/>, or <see langword="null"/> if the game has not been initialized.
    /// </value>
    IBoard? Board { get; set; }
    /// <summary>
    /// Gets or sets the "card base", containing all <see cref="ICard"/>s, <see cref="ICardSet"/>s, <see cref="Deck"/>s, maps between them, and associated methods.
    /// </summary>
    /// <value>
    /// A <see cref="CardBase"/> instance if initialized; if not, <see langword="null"/>.
    /// </value>
    CardBase? Cards { get; set; }
    /// <summary>
    /// Gets or sets a service which "regulates" interaction between the model and the players (interprets player actions according to the game state, then executes logic according to game rules).
    /// </summary>
    /// <value>
    /// An instance of <see cref="IBoard"/>, or <see langword="null"/> if the game has not been initialized.
    /// </value>
    IRegulator? Regulator { get; set; }
    /// <summary>
    /// Gets or sets a data object containing game-specific rules values, like continent bonuses or equations for bonus armies.
    /// </summary>
    /// <value>
    /// An instance of <see cref="IRuleValues"/>, or <see langword="null"/> if the game has not been initialized.
    /// </value>
    IRuleValues? Values { get; set; }

    /// <summary>
    /// Initialize the game model given a list of Player names.
    /// </summary>
    /// <param name="names">The names provided by the player.</param>
    abstract void Initialize(string[] names);
    /// <summary>
    /// Initialize the game model from a saved game file.
    /// </summary>
    /// <param name="openStream">A stream opened on a saved game file. Its content and format are dictated by <see cref="Save"/>.</param>
    abstract void Initialize(FileStream openStream);
    /// <summary>
    /// Save game state to a file.
    /// </summary>
    /// <param name="isNewFile">A boolean indicating whether the save file is new.</param>
    /// <param name="fileName">The name of the save file.</param>
    /// <param name="precedingData">Data from the View and/or ViewModel to be written to the save file first.</param>
    abstract Task Save(bool isNewFile, string fileName, string precedingData);

}