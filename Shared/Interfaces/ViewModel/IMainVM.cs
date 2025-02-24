using Shared.Enums;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Shared.Interfaces.ViewModel;
/// <summary>
/// Defines public exposures of the main ViewModel. Enables decoupled DI injection into the primary View.
/// </summary>
public interface IMainVM : IBinarySerializable
{
    #region Properties
    /// <summary>
    /// Gets or sets the current Game.
    /// </summary>
    /// <value>
    /// The current <see cref="IGame"/> instance if both it and the <see cref="IMainVM"/> have been initialized; otherwise <see langword="null"/>. <br/>
    /// </value>
    IGame? CurrentGame { get; set; }
    /// <summary>
    /// Gets or sets the current game phase.
    /// </summary>
    GamePhase CurrentPhase { get; set; }
    /// <summary>
    /// Gets or sets the currently selected territory.
    /// </summary>
    TerrID TerritorySelected { get; set; }
    /// <summary>
    /// Gets or sets a collection of territory information for display.
    /// </summary>
    ObservableCollection<ITerritoryInfo> Territories { get; set; }
    /// <summary>
    /// Gets or sets a collection of player information for display.
    /// </summary>
    ObservableCollection<IPlayerData> PlayerDetails { get; set; }
    /// <summary>
    /// Gets a map of <see cref="ContID"/> to their names.
    /// </summary>
    /// <remarks>
    /// A cache that helps avoid multiple <see cref="Enum"/> method calls.
    /// </remarks>
    ReadOnlyDictionary<ContID, string> ContNameMap { get; }
    /// <summary>
    /// Gets or sets the Path to the directory of the Application.
    /// </summary>
    /// <remarks>
    /// Provided by the Dependency Injection system via <see cref="Microsoft.Extensions.Options"/>.
    /// </remarks>
    string AppPath { get; set; }
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
    /// The <see cref="IPlayer.Number"/> whose turn it is. <br/>
    /// </value>
    int PlayerTurn { get; set; }
    /// <summary>
    /// Gets the number of bonus armies that will be awarded on the next card trade-in.
    /// </summary>
    int NextTradeBonus { get; }

    /// <summary>
    /// Gets the new game command.
    /// </summary>
    ICommand NewGame_Command { get; }
    /// <summary>
    /// Gets the save game command.
    /// </summary>
    ICommand SaveGame_Command { get; }
    /// <summary>
    /// Gets the load game command.
    /// </summary>
    ICommand LoadGame_Command { get; }
    /// <summary>
    /// Gets the territory select command.
    /// </summary>
    ICommand TerritorySelect_Command { get; }
    /// <summary>
    /// Gets the trade-in command.
    /// </summary>
    ICommand TradeIn_Command { get; }
    /// <summary>
    /// Gets the troop advance command.
    /// </summary>
    ICommand Advance_Command { get; }
    /// <summary>
    /// Gets the deliver reward command.
    /// </summary>
    ICommand DeliverAttackReward_Command { get; }
    /// <summary>
    /// Gets the undo confirm input command.
    /// </summary>
    ICommand UndoConfirmInput_Command { get; }
    /// <summary>
    /// Gets the choose territory bonus command.
    /// </summary>
    ICommand ChooseTerritoryBonus_Command { get; }
    #endregion

    #region Events
    /// <summary>
    /// Fires if the turn is changing control between players.
    /// </summary>
    /// <remarks>
    /// Warns the View that the Player Turn will soon change; this is needed in a single-seat multiplayer game so that players can keep information hidden.
    /// </remarks>
    event EventHandler<int>? PlayerTurnChanging;
    /// <summary>
    /// Fires when a player input is needed to choose between territories. 
    /// </summary>
    /// <remarks>
    /// See <see cref="IRegulator.PromptBonusChoice"/>.
    /// </remarks>
    event EventHandler<ValueTuple<int, string>[]> TerritoryChoiceRequest;
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
    #endregion
    /// <summary>
    /// Initializes the MainViewModel using either newly input values or the name of a save file.
    /// </summary>
    /// <param name="players">The players in the new game.</param>
    /// <param name="colors">The color names for the players in the new game.</param>
    /// <param name="fileName">The name of the save file to use, if any; otherwise, <see langword="null"/>.</param>
    abstract void Initialize(string[] players, string[] colors, string? fileName);
    /// <summary>
    /// Executes logic of the <see cref="NewGame_Command"/>.
    /// </summary>
    /// <param name="namesAndColors">The names and colors of the new players.</param>
    abstract void NewGame(ValueTuple<string, string>[] namesAndColors);
    /// <summary>
    /// Executes logic of the <see cref="SaveGame_Command"/>.
    /// </summary>
    /// <param name="saveParams">The file name to save to, and a flag indicating whether it is a new file.</param>
    abstract Task SaveGame((string FileName, bool NewFile) saveParams);
    /// <summary>
    /// Executes logic of the <see cref="LoadGame_Command"/>.
    /// </summary>
    /// <param name="fileName">The name of the file from which to load.</param>
    abstract void LoadGame(string fileName);
    /// <summary>
    /// Executes logic of the <see cref="TerritorySelect_Command"/>.
    /// </summary>
    /// <param name="territory">The ID of the territory selected.</param>
    abstract void TerritorySelect(int territory);
    /// <summary>
    /// Executes logic of the <see cref="ChooseTerritoryBonus_Command"/>.
    /// </summary>
    /// <param name="target">The underlying value of the target territory's <see cref="TerrID"/>.</param>
    abstract void ChooseTerritoryBonus(int target);
    /// <summary>
    /// Sums the total number of armies owned by a player.
    /// </summary>
    /// <param name="player">The <see cref="IPlayer.Number"/> of the player whose armies will be summed.</param>
    /// <returns>A positive <see cref="int"/>.</returns>
    int SumArmies(int player);
    /// <summary>
    /// Sums the total number of territories controlled by a player.
    /// </summary>
    /// <param name="player">The <see cref="IPlayer.Number"/> of the player whose territories will be counted.</param>
    /// <returns>A positive <see cref="int"/>.</returns>
    int SumTerritories(int player);
    /// <summary>
    /// Makes names suitable for display in the UI.
    /// </summary>
    /// <param name="name">The name to be amended.</param>
    /// <returns>The name, edited for display.</returns>
    string MakeDisplayName(string name);
}
