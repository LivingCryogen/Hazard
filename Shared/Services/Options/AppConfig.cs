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
    /// In developer mode, is should be set to <see cref="Environment.CurrentDirectory"/>; in production, the package manager (since containerized).
    /// </remarks>
    public string AppPath { get; set; } = string.Empty;
}
