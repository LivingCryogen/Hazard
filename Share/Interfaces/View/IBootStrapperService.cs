namespace Share.Interfaces.View;
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
    /// <param name="fileName">The <see cref="string">name</see> of the file from which to load.</param>
    void InitializeGame(string fileName);
    /// <summary>
    /// Boots from user provided data.
    /// </summary>
    /// <remarks>
    /// Typically this means when a "New Game" is launched with an <see cref="Model.IGame"/> instance already running.
    /// </remarks>
    /// <param name="namesAndColors">An array of <see cref="Tuple{T1, T2}"/>, where each T1 contains the name of a new player, and T2 the name of the color they selected.</param>
    void InitializeGame((string Name, string Color)[] namesAndColors);
}