using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Services.Configuration;

/// <summary>
/// Unique Installation identification data.
/// </summary>
public class InstallationInfo
{
    /// <summary>
    /// Gets or sets a unique identifier naming this machine/installation.
    /// </summary>
    /// <remarks>
    /// Generated on first run. Used by Azure Database; (Player Name + InstallID) will form Table Key.
    /// </remarks>
    public Guid InstallID { get; set; }
    /// <summary>
    /// Gets or sets the Date and Time the application is first run.
    /// </summary>
    public DateTime FirstRun { get; set; }
}
