namespace Shared.Interfaces.View;

/// <summary>
/// Provides start-up/initialization in response to New and Load Game Commands.
/// </summary>
public interface IAppCommander
{
    /// <summary>
    /// The save file name of the current Game, if any.
    /// </summary>
    /// <value>
    /// If the current Game was saved to or loaded from a save file, its name; otherwise, <see cref="string.Empty"/>.
    /// </value>
    public string SaveFileName { get; set; }
    /// <summary>
    /// Initializes an empty game.
    /// </summary>
    public void InitializeGame();
    /// <summary>
    /// Initializes a new Game from a save file (loads a game).
    /// </summary>
    /// <param name="fileName"></param>
    public void InitializeGame(string fileName);
    /// <summary>
    /// Initializes a new Game with new game settings.
    /// </summary>
    /// <param name="namesAndColors"></param>
    public void InitializeGame((string Name, string Color)[] namesAndColors);
}
