namespace Shared.Services.Options;
/// <summary>
/// Contains values needed for run-time Options to be set and configured by the DI system.
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Gets or sets the path of the current running Application's root folder.
    /// </summary>
    /// <remarks>
    /// In developer mode, it should be set by <see cref="Path.GetDirectoryName(string?)"/> of <see cref="System.Reflection.Assembly.GetExecutingAssembly"/>; in production, to the MSIX package installation path (since containerized).
    /// </remarks>
    public string AppPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the path of the save file associated with the running Application.
    /// </summary>
    /// <value>
    /// The application's current save file path; if a game has not been loaded or saved yet, <see cref="string.Empty"/>.
    /// </value>
    public string SaveFileName { get; set; } = string.Empty;
}
