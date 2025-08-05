using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model.Stats.Services;
using Shared.Services.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats.Repository;

public class StatRepo(IOptions<AppConfig> options, ILogger<StatRepo> logger)
{
    private readonly ILogger<StatRepo> _logger = logger;
    private readonly string StatFilePath = options.Value.StatRepoFilePath;
    private readonly Dictionary<Guid, string> _gamePaths = [];
    private readonly Dictionary<Guid, int> _gameUpdateCounts = [];

    /// <summary>
    /// Adds Game persistence mappings, or updates them if they already exist, using a Game's Stat Tracker.
    /// </summary>
    /// <param name="tracker">The stat tracker of the game whose entries should be added or updated.</param>
    public void AddOrUpdate(StatTracker tracker)
    {
        if (tracker.LastSavePath == null)
        {
            if (!_gamePaths.TryGetValue(tracker.GameID, out string? mappedPath))
            {
                _logger.LogError("{Repo} attempted AddOrUpdate with {Tracker} for Game {ID}, but its LastSavePath was null.", this, tracker, tracker.GameID);
                return;
            }

            _logger.LogWarning("{Repo} attempted AddOrUpdate with {Tracker} for Game {ID}, but its LastSavePath was null. Proceeding with previous path: {path}."
                , this, tracker, tracker.GameID, mappedPath);

        }

        if (_gamePaths.TryAdd(tracker.GameID, tracker.LastSavePath))
        {
            if (!_gameUpdateCounts.TryAdd(tracker.GameID, 1))
            {
                _logger.LogWarning("{Repo} attempted to add a new Update Count entry for Game {ID}, but it already had one!", this, tracker.GameID);
            }
            else if (!tracker.AwaitsUpdate)
            {
                _logger.LogWarning("{Repo} was called to AddOrUpdate {tracker}, but its AwaitsUpdate flag was set to false. Update skipped.", this, tracker);
                _gameUpdateCounts[tracker.GameID]++;
            }
            else
            { }
        }
    }
}
