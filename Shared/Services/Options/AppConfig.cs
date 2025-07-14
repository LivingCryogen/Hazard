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
    /// Gets or sets the path to the Game Statistics repository file.
    /// </summary>
    /// <value>
    /// 'GameStatistics.hzd' + AppPath unless otherwise specified in 'appsettings.json.'
    /// </value>
    public string StatRepoFilePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the Version Number of the Statistics records to be used.
    /// </summary>
    public int StatVersion { get; set; }
    /// <summary>
    /// Gets or sets a map of data file paths keyed by their filenames (with extension).
    /// </summary>
    /// <value>
    /// Each key should be a data file name with extension, read from the 'DataFileNames' property of 'appsettings.json.' <br/>
    /// Their values are the full, absolute paths to these files, determined at runtime.
    /// </value>
    public Dictionary<string, string> DataFileMap { get; set; } = [];
    /// <summary>
    /// Gets or sets a map of sound file paths keyed by their filenames (with extension).
    /// </summary>
    /// <value>
    /// Each key should be a sound file name with extension, read from the 'SoundFileNames' property of 'appsettings.json.' <br/>
    /// Their values are the full, absolute paths to these files, determined at runtime.
    /// </value>
    public Dictionary<string, string> SoundFileMap { get; set; } = [];
    /// <summary>
    /// Gets or sets the string used to search for CardSet datafiles at startup..
    /// </summary>
    /// <remarks>
    /// See <see cref="Model.DataAccess.AssetFetcher.FetchCardSets"/>
    /// </remarks>
    public string CardDataSearchString { get; set; } = string.Empty;
}
