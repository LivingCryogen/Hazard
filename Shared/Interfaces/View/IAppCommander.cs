namespace Shared.Interfaces.View;

/// <summary>
/// Provides start-up/initialization in response to New and Load Game Commands.
/// </summary>
public interface IAppCommander
{
    /// <summary>
    /// The save file path of the current Game, if any.
    /// </summary>
    /// <value>
    /// If the current Game was saved to or loaded from a save file, its path; otherwise, <see cref="string.Empty"/>.
    /// </value>
    public string SaveFilePath { get; set; }
    /// <summary>
    /// Initializes the Application 's Main Window and its ViewModel, either from a save file or from new game user selections, if any. Defaults to an uninitialized ViewModel.
    /// </summary>
    /// <remarks>
    /// Unitialized ViewModel still allows access to Options and Statistics, but not to game functionality.
    /// </remarks>
    /// <param name="fileName">The path to the save file, if any.</param>
    /// <param name="userSelections">A list of player name and color pairs provided by the user, if any.</param>
    public void Initialize(string? fileName, (string Name, string Color)[]? userSelections);
}
