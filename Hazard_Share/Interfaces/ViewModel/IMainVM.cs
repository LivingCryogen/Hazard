using Hazard_Share.Enums;
using Hazard_Share.Interfaces.Model;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Hazard_Share.Interfaces.ViewModel;
/// <summary>
/// Defines public exposures of the main ViewModel. Enables decoupled DI injection into the primary View.
/// </summary>
public interface IMainVM
{
    /// <summary>
    /// Gets or inits the current <see cref="IGame"/>.
    /// </summary>
    /// <value>
    /// The current <see cref="IGame"/> instance if both it and the <see cref="IMainVM"/> have been initialized; otherwise <see langword="null"/>. <br/>
    /// See <see cref="IGame.Initialize(string[])"/>, <see cref="IGame.Initialize(FileStream)"/>, and <see cref="IMainVM.Initialize(string)"/>, <see cref="IMainVM.Initialize(ValueTuple{string, string}[])"/>.
    /// </value>
    IGame? CurrentGame { get; init; }
    /// <summary>
    /// Gets or sets the current game phase.
    /// </summary>
    /// <value>
    /// A <see cref="GamePhase"/>.
    /// </value>
    GamePhase CurrentPhase { get; set; }
    /// <summary>
    /// Gets or sets the currently selected territory.
    /// </summary>
    /// <value>
    /// A <see cref="TerrID"/>.
    /// </value>
    TerrID TerritorySelected { get; set; }
    /// <summary>
    /// Gets or sets a collection of territory information for display.
    /// </summary>
    /// <value>
    /// An <see cref="ObservableCollection{T}"/>, where T is <see cref="ITerritoryInfo"/>.
    /// </value>
    ObservableCollection<ITerritoryInfo> Territories { get; set; }
    /// <summary>
    /// Gets or sets a collection of player information for display.
    /// </summary>
    /// <value>
    /// An <see cref="ObservableCollection{T}"/>, where T is <see cref="IPlayerData"/>.
    /// </value>
    ObservableCollection<IPlayerData> PlayerDetails { get; set; }
    /// <summary>
    /// Gets a map of <see cref="ContID"/> to their <see cref="string">name</see>s.
    /// </summary>
    /// <remarks>
    /// Functions as a cache so as to avoid multiple <see cref="Enum"/> method calls.
    /// </remarks>
    ReadOnlyDictionary<ContID, string> ContNameMap { get; }
    /// <summary>
    /// Gets or sets a flag indicating whether the attack command should be enabled.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the application is ready to accept another attack input; otherwise, <see langword="false" />.
    /// </value>
    bool AttackEnabled { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whose turn it is.
    /// </summary>
    /// <value>
    /// An <see cref="int"/>, 0-5, representing the current <see cref="IPlayer"/> whose turn it is. <br/>
    /// See <see cref="IPlayer.Number"/> and <see cref="Hazard_Model.Core.StateMachine.PlayerTurn"/>.
    /// </value>
    int PlayerTurn { get; set; }
    /// <summary>
    /// Gets the number of bonus armies that will be awarded on the next card trade-in.
    /// </summary>
    /// <value>
    /// An <see cref="int"/>.
    /// </value>
    int NextTradeBonus { get; }

    /// <summary>
    /// Gets the new game command.
    /// </summary>
    /// <value>
    /// An <see cref="ICommand"/>.
    /// </value>
    ICommand NewGame_Command { get; }
    /// <summary>
    /// Gets the save game command.
    /// </summary>
    /// <value>
    /// An <see cref="ICommand"/>.
    /// </value>
    ICommand SaveGame_Command { get; }
    /// <summary>
    /// Gets the load game command.
    /// </summary>
    /// <value>
    /// An <see cref="ICommand"/>.
    /// </value>
    ICommand LoadGame_Command { get; }
    /// <summary>
    /// Gets the territory select command.
    /// </summary>
    /// <value>
    /// An <see cref="ICommand"/>.
    /// </value>
    ICommand TerritorySelect_Command { get; }
    /// <summary>
    /// Gets the trade-in command.
    /// </summary>
    /// <value>
    /// An <see cref="ICommand"/>.
    /// </value>
    ICommand TradeIn_Command { get; }
    /// <summary>
    /// Gets the troop advance command.
    /// </summary>
    /// <value>
    /// An <see cref="ICommand"/>.
    /// </value>
    ICommand Advance_Command { get; }
    /// <summary>
    /// Gets the deliver reward command.
    /// </summary>
    /// <value>
    /// An <see cref="ICommand"/>.
    /// </value>
    ICommand DeliverAttackReward_Command { get; }
    /// <summary>
    /// Gets the undo confirm input command.
    /// </summary>
    /// <value>
    /// An <see cref="ICommand"/>.
    /// </value>
    ICommand UndoConfirmInput_Command { get; }
    /// <summary>
    /// Gets the choose territory bonus command.
    /// </summary>
    /// <value>
    /// An <see cref="ICommand"/>.
    /// </value>
    ICommand ChooseTerritoryBonus_Command { get; }

    /// <summary>
    /// Fires if the turn is changing control between players.
    /// </summary>
    /// <remarks>
    /// Warns the View that the Player Turn will change; this is needed in a single-seat multiplayer game so that players can keep information hidden.
    /// </remarks>
    event EventHandler<int>? PlayerTurnChanging;
    /// <summary>
    /// Fires when a player input is needed to choose between territories. 
    /// </summary>
    /// <remarks>
    /// See <see cref="Hazard_Model.Core.Regulator.PromptBonusChoice"/>.
    /// </remarks>
    event EventHandler<Tuple<int, string>[]> TerritoryChoiceRequest;
    /// <summary>
    /// Fires when the application should deliver a prompt allowing a player to trade in their cards.
    /// </summary>
    event EventHandler<int>? RequestTradeIn;
    /// <summary>
    /// Fires when the application should force a player to turn in a matching set of cards before continuing.
    /// </summary>
    event EventHandler<int>? ForceTradeIn;
    /// <summary>
    /// Fires when the Model layer asks for an attack to be performed.
    /// </summary>
    /// <remarks>
    /// Currently, due to the way dice rolling, results, and animations are tied together, the actual result of random number generation <br/>
    /// is given in the ViewModel, which then passes those results on to the Model for it to execute logic with them. This might need <br/>
    /// to be changed in the future, but would require rethinking how to give a player a "real" view of the "roll."
    /// </remarks>
    event EventHandler<int>? AttackRequest;
    /// <summary>
    /// Fires when user input is needed to determine how many armies will move after a successful attack.
    /// </summary>
    event EventHandler<ITroopsAdvanceEventArgs>? AdvanceRequest;
    /// <summary>
    /// Fires when the "dice rolling" process has concluded.
    /// </summary>
    event EventHandler<IDiceThrownEventArgs>? DiceThrown;
    /// <summary>
    /// Fires when a player is defeated.
    /// </summary>
    event EventHandler<int>? PlayerLost;
    /// <summary>
    /// Fires when a player wins.
    /// </summary>
    event EventHandler<int>? PlayerWon;
    /// <summary>
    /// Initializes an <see cref="IMainVM"/> using names and color names provided by a user.
    /// </summary>
    /// <param name="namesAndColors">An array of <see cref="Tuple{T1, T2}"/>, where T1 is a <see cref=" string"/> player name, and T2 is a <see cref="string">name</see> of their color.</param>
    abstract void Initialize((string Name, string ColorName)[] namesAndColors);
    /// <summary>
    /// Initializes an <see cref="IMainVM"/> from a save file.
    /// </summary>
    /// <param name="fileName">The <see cref="string">name</see> of the file from which to initialize.</param>
    abstract void Initialize(string fileName);
    /// <summary>
    /// Executes logic of the <see cref="NewGame_Command"/>.
    /// </summary>
    /// <param name="parameter"></param>
    abstract void NewGame(object? parameter);
    /// <summary>
    /// Executes logic of the <see cref="SaveGame_Command"/>.
    /// </summary>
    /// <param name="saveParams">A <see cref="Tuple{T1, T2}"/>, where T1 is the <see cref="string"/> file name, and T2 is a <see cref="bool"/> indicating whether it is a new file.</param>
    abstract void SaveGame((string FileName, bool NewFile) saveParams);
    /// <summary>
    /// Executes logic of the <see cref="LoadGame_Command"/>.
    /// </summary>
    /// <param name="fileName">The <see cref="string">name</see> of the file from which to load.</param>
    abstract void LoadGame(string fileName);
    /// <summary>
    /// Executes logic of the <see cref="TerritorySelect_Command"/>.
    /// </summary>
    /// <param name="territory">The <see cref="TerrID"/> of the territory selected.</param>
    abstract void TerritorySelect(int territory);
    /// <summary>
    /// Executes logic of the <see cref="ChooseTerritoryBonus_Command"/>.
    /// </summary>
    /// <param name="target">The <see cref="int"/> value of the selected territory's <see cref="TerrID"/>.</param>
    abstract void ChooseTerritoryBonus(int target);
    /// <summary>
    /// Sums the total number of armies owned by a player.
    /// </summary>
    /// <param name="player">An <see cref="int"/> corresponding to the <see cref="IPlayer.Number"/> of the player whose armies will be summed.</param>
    /// <returns>An <see cref="int"/>.</returns>
    int SumArmies(int player);
    /// <summary>
    /// Sums the total number of territories controlled by a player.
    /// </summary>
    /// <param name="player">An <see cref="int"/> corresponding to the <see cref="IPlayer.Number"/> of the player whose territories will be counted.</param>
    /// <returns>An <see cref="int"/>.</returns>
    int SumTerritories(int player);
    /// <summary>
    /// Makes names suitable for display in the UI.
    /// </summary>
    /// <param name="name">The <see cref="string">name</see> to be amended.</param>
    /// <returns>A <see cref="string"/>.</returns>
    string MakeDisplayName(string name);
}
