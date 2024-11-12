namespace Shared.Interfaces.View;
/// <summary>
/// Provides methods for booting up the application.
/// </summary>
public interface IBootStrapperService
{
    /// <summary>
    /// Gets or sets the name of the save file associated with the current <see cref="Model.IGame"/>
    /// </summary>
    string SaveFileName { get; set; }
    /// <summary>
    /// A clean boot; occurs the first time the application is launched.
    /// </summary>
    void InitializeGame();
    /// <summary>
    /// Boots from a saved file.
    /// </summary>
    /// <param name="fileName">The name of the file from which to load.</param>
    void InitializeGame(string fileName);
    /// <summary>
    /// Boots from user provided data.
    /// </summary>
    /// <remarks>
    /// Typically, used when a "New Game" is launched with an <see cref="Model.IGame"/> instance already running.
    /// </remarks>
    /// <param name="namesAndColors">The name and color pairs for each player.</param>
    void InitializeGame((string Name, string Color)[] namesAndColors);
}