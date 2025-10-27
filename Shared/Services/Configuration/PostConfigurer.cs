using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Shared.Services.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Shared.Services.Configuration;

/// <summary>
/// Post-configures AppConfig options after initial binding.
/// </summary>
/// <remarks>
/// Essential for runtime configuration (e.g., file paths, install info) that cannot be set during initial binding.
/// This custom class and method is necessary to enable logging during post-configuration.
/// </remarks>
/// <param name="logger">A logger from DI.</param>
/// <param name="env">Host environment from DI.</param>
public class PostConfigurer(ILogger<IPostConfigureOptions<AppConfig>> logger, IHostEnvironment env) : IPostConfigureOptions<AppConfig>
{
    private readonly ILogger<IPostConfigureOptions<AppConfig>> _logger = logger;
    private readonly string _appPath = env.ContentRootPath;

    /// <summary>
    /// Post-configures the AppConfig options.
    /// </summary>
    /// <remarks>
    /// Should be automatically called by the options framework after initial configuration binding.
    /// </remarks>
    /// <param name="name">The name of the options instance being configured. <c>null</c> for default options.</param>
    /// <param name="options">The options instance to be post-configured.</param>
    /// <exception cref="FileNotFoundException">
    /// Thrown if a required file is missing or inaccessible during post-configuration.
    /// </exception>
    public void PostConfigure(string? name, AppConfig options)
    {
        // Generate and store install-unique information on first run (or if otherwise missing the file). To be used as identifier in Azure DataBase
        string installInfoPath = Path.Combine(_appPath, "installation.json");
        InstallationInfo installationInfo;

        if (env.IsDevelopment())
            options.DevMode = true;

        if (File.Exists(installInfoPath))
        {
            string json = File.ReadAllText(installInfoPath);
            installationInfo = JsonSerializer.Deserialize<InstallationInfo>(json) ?? new InstallationInfo();
        }
        else
        {
            installationInfo = new InstallationInfo()
            {
                InstallId = Guid.NewGuid(),
                FirstRun = DateTime.UtcNow
            };

            string installJson = JsonSerializer.Serialize(installationInfo,
                new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            File.WriteAllText(installInfoPath, installJson);
        }

        options.InstallInfo.InstallId = installationInfo.InstallId;
        options.InstallInfo.FirstRun = installationInfo.FirstRun;
        options.AppPath = _appPath;

        var findRepoPaths = DataFileFinder.FindFiles(_appPath, options.StatRepoFileName);
        if (findRepoPaths == null || findRepoPaths.Length == 0)
        {
            var statsDir = Path.Combine(_appPath, "Stats");
            if (!Directory.Exists(statsDir))
                Directory.CreateDirectory(statsDir);

            var statRepoPath = Path.Combine(statsDir, options.StatRepoFileName);
            using (File.Create(statRepoPath)) { }
            options.StatRepoFilePath = statRepoPath;
        }
        else
        {
            options.StatRepoFilePath = findRepoPaths[0];
        }

        foreach (string dataFileName in options.DataFileNames)
        {
            var dataFileLocations = DataFileFinder.FindFiles(_appPath, dataFileName);
            if (dataFileLocations == null || dataFileLocations.Length == 0)
                throw new FileNotFoundException($"Required data file '{dataFileName}' not found in application directory or subdirectories.");
            if (dataFileLocations.Length > 1)
                _logger.LogWarning("Multiple files found for data file '{dataFileName}'. Using first found at '{dataFileLocation}'.", dataFileName, dataFileLocations[0]);

            var dataFile = dataFileLocations[0];
            options.DataFileMap.Add(dataFileName, dataFile);
        }
        foreach (string soundFileName in options.SoundFileNames)
        {
            var soundFileLocations = DataFileFinder.FindFiles(_appPath, soundFileName);
            if (soundFileLocations == null || soundFileLocations.Length == 0)
            {
                _logger.LogWarning("Required sound file '{soundFileName}' not found in application directory or subdirectories.", soundFileName);
            }
            else if (soundFileLocations.Length > 1)
            {
                _logger.LogWarning("Multiple files found for sound file '{soundFileName}'. Using first found at '{soundFilePath}'.", soundFileName, soundFileLocations[0]);
                var soundFile = soundFileLocations[0];
                options.SoundFileMap.Add(soundFileName, soundFile);
            }
            else
            {
                var soundFile = soundFileLocations[0];
                options.SoundFileMap.Add(soundFileName, soundFile);
            }
        }
    }
}
