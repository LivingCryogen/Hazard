using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model.Core;
using Model.Stats.Services;
using Model.Stats.StatModels;
using Shared.Interfaces.Model;
using Shared.Services.Configuration;
using Shared.Services.Serializer;
using System.Text.Json;

namespace Model.Stats;
/// <summary>
/// 
/// </summary>
/// <param name="connectionHandler"></param>
/// <param name="statFactory"></param>
/// <param name="options"></param>
/// <param name="loggerFactory"></param>
/// <param name="logger"></param>
public class StatRepo(WebConnectionHandler connectionHandler,
    Func<IStatTracker> statFactory,
    IOptions<AppConfig> options,
    ILoggerFactory loggerFactory,
    ILogger<StatRepo> logger) : IBinarySerializable, IStatRepo
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly ILogger<StatRepo> _logger = logger;
    private readonly string StatFilePath = options.Value.StatRepoFilePath;
    private readonly string StatsFolderName = Path.GetDirectoryName(options.Value.StatRepoFilePath) ?? "/";
    private readonly WebConnectionHandler _connectionHandler = connectionHandler;
    private readonly Func<IStatTracker> _statTrackerFactory = statFactory;
    private Dictionary<Guid, SavedStatMetadata> _gameStats = [];
    private int _pendingSyncs = 0;

    /// <inheritdoc cref="IStatRepo.CurrentTracker" />
    public IStatTracker? CurrentTracker { get; set; }
    /// <inheritdoc cref="IStatRepo.SyncPending"/>
    public string SyncStatusMessage { get; private set; } = "Sync";
    /// <summary>
    /// Gets a flag indicating whether there are any syncs (game statistics updates) pending.
    /// </summary>
    public bool SyncPending { get => _pendingSyncs > 0; }

    /// <inheritdoc cref="IStatRepo.Update(string, ValueTuple{string, long}[])"/>
    public async Task<bool> Update(string lastSavePath, (string, long)[] objNamesAndPositions)
    {
        try
        {
            if (CurrentTracker == null)
                return false;

            Guid gameID = CurrentTracker.GameID;

            // parse Save result for the streamPosition of StatTracker
            var statTrackerResult = objNamesAndPositions.Where(np => np.Item1 == nameof(StatTracker)).FirstOrDefault();
            if (statTrackerResult == default)
            {
                _logger.LogWarning("StatTracker not found in save results");
                return false;
            }
            long trackerPosition = statTrackerResult.Item2;

            if (string.IsNullOrEmpty(lastSavePath))
            {
                _logger.LogWarning("Stat Repo was asked to update stats, but was provided an invalid save path.");
                return false;
            }
            
            if (CurrentTracker.TrackedActions <= 0)
            {
                _logger.LogInformation("Stat Repo skipped update for game {id}, because it had no tracked actions.", gameID);
                return false;
            }

            if (_gameStats.TryGetValue(gameID, out var value) && value is SavedStatMetadata oldData)
            {
                if (oldData.ActionCount >= CurrentTracker.TrackedActions)
                {
                    _logger.LogInformation("Stat Repo skipped update for game {id}, because it had no new tracked actions: {old} vs {new}.", gameID, oldData.ActionCount, CurrentTracker.TrackedActions);
                    return false;
                }
                else
                {
                    var updatedData = new SavedStatMetadata(_loggerFactory.CreateLogger<SavedStatMetadata>())
                    {
                        SavePath = lastSavePath,
                        ActionCount = CurrentTracker.TrackedActions,
                        StreamPosition = trackerPosition,
                        SyncPending = true
                    };
                    _gameStats[gameID] = updatedData;
                    _pendingSyncs++;
                }
            }
            else if (_gameStats.TryAdd(gameID, new(_loggerFactory.CreateLogger<SavedStatMetadata>())
                {
                    SavePath = lastSavePath,
                    ActionCount = CurrentTracker.TrackedActions,
                    StreamPosition = trackerPosition,
                    SyncPending = true
                })) 
            {
                _pendingSyncs++;
                _logger.LogInformation("Stat Repo added new tracked game {id} with {actions} actions.", gameID, CurrentTracker.TrackedActions);
            }
            else
            {
                _logger.LogWarning("Stat repo failed to add stats for game: {id}. Aborting update.", gameID);
                return false;
            }

            try
            {
                await Save();
            }
            catch (Exception ex)
            {
                _logger.LogError("There was an unexpected error when attempting to write Stat Repo after updating game {id}: {message}", gameID, ex.Message);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("There was an unexpected error when attempting to update Stat Repo: {Message}", ex.Message);
            return false;
        }   
    }

    /// <inheritdoc cref="IStatRepo.FinalizeCurrentGame"/>
    public async Task<bool> FinalizeCurrentGame()
    {
        if (CurrentTracker == null)
        {
            _logger.LogWarning("Stat Repo was asked to finalize a game, but no current Stat Tracker was set.");
            return false;
        }

        if (!CurrentTracker.Completed)
        {
            _logger.LogWarning("Stat Repo was asked to finalize a game, but the current Stat Tracker was not marked as completed.");
            return false;
        }

        string completedPath = GetCompletedStatPath(CurrentTracker.GameID);
        try
        {
            await BinarySerializer.Save([CurrentTracker], completedPath, true);
        }
        catch (Exception ex)
        {
            _logger.LogError("There was an unexpected error when attempting to write final stat file for game {id}: {message}", CurrentTracker.GameID, ex.Message);
            return false;
        }

        var completedMetaData = new SavedStatMetadata(_loggerFactory.CreateLogger<SavedStatMetadata>())
        {
            SavePath = completedPath,
            StreamPosition = 0,
            ActionCount = CurrentTracker.TrackedActions,
            SyncPending = true
        };

        // Try to find previous metadata for this game
        if (_gameStats.TryGetValue(CurrentTracker.GameID, out var prevMetaData))
        {
            if (prevMetaData == null)
            {
                _logger.LogWarning("Stat Repo was asked to finalize a game with a valid game ID key, but SavedMetaData for game {gameID} could not be found.", CurrentTracker.GameID);
                _pendingSyncs++;
            }
            else if (!prevMetaData.SyncPending)
            { 
                _pendingSyncs++;
            }
            // else : previous metadata was already pending sync, so no need to increment
        }
        else
            _pendingSyncs++;

        _gameStats[CurrentTracker.GameID] = completedMetaData;

        try
        {
            await Save();
        }
        catch (Exception ex)
        {
            _logger.LogError("There was an unexpected error when attempting to write Stat Repo after finalizing game {id}: {message}", CurrentTracker.GameID, ex.Message);
            return false;
        }


        _logger.LogInformation("Game {id} was finalized and marked for sync at {path}.", CurrentTracker.GameID, completedPath);

        CurrentTracker = null;

        return true;
    }

    private string GetCompletedStatPath(Guid gameID)
    {
        return Path.Combine(StatsFolderName, "completegame" + gameID.ToString() + ".stat");
    }

    /// <inheritdoc cref="IStatRepo.SyncToAzureDB"/>
    public async Task<bool> SyncToAzureDB()
    {
        if (!SyncPending)
        {
            SyncStatusMessage = "Sync";
            _logger.LogInformation("A database sync was called without pending changes.");
            return false;
        }

        SyncStatusMessage = "Syncing...";
        SyncStatusMessage = "Verifying Internet Connectivity...";

        if (!await _connectionHandler.VerifyInternetConnection())
        {
            _logger.LogInformation("A database sync was cancelled after a failure to verify internet connectivity.");
            SyncStatusMessage = "Failed Sync: No Internet";
            return false;
        }

        SyncStatusMessage = "Verifying Proxy Connection...";

        if (!await _connectionHandler.VerifyAzureWebAppConnection())
        {
            _logger.LogInformation("A database sync was cancelled after a failure to verify Proxy Server connectivity.");
            SyncStatusMessage = "Failed Sync: No Proxy";
            return false;
        }

        SyncStatusMessage = "Syncing...";

 
        try
        {
            bool allSessionsSynced = true;
            List<Guid> failedSyncs = [];

            foreach (Guid gameID in _gameStats.Keys)
            {
                var metaData = (_gameStats.TryGetValue(gameID, out var data) && data is not null) ? data : null;
                if (metaData == null)
                {
                    _logger.LogWarning("Game {ID} skipped, no metadata found.", gameID);
                    continue;
                }

                if (!_gameStats[gameID].SyncPending)
                {
                    _logger.LogInformation("Game {ID} skipped, not pending sync.", gameID);
                    continue;
                }    

                SyncStatusMessage = $"Syncing game {gameID}...";

                bool synced = await SyncGameSession(gameID);

                if (synced)
                {
                    _logger.LogInformation("Succesfully synced game {ID}", gameID);
                    metaData.SyncPending = false;
                    _pendingSyncs--;
                    if (_pendingSyncs < 0)
                    {
                        _logger.LogWarning("Pending syncs counter dropped below zero. Resetting to zero.");
                        _pendingSyncs = 0;
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to sync game {ID}!", gameID);
                    failedSyncs.Add(gameID);
                    allSessionsSynced = false;
                }
            }

            if (allSessionsSynced)
            {
                SyncStatusMessage = "Sync Completed Succesfully!";
                _logger.LogInformation("Sync completed succesfully on {count} games at {time}", _pendingSyncs, DateTime.Now);
                await Save();
                return true;
            }
            else
            {
                SyncStatusMessage = $"Sync Succeeded for {_pendingSyncs - failedSyncs.Count} games; Failed for {failedSyncs.Count}.";
                _logger.LogInformation("Sync failed for {failednum} of {total} games at {time}.", failedSyncs.Count, _pendingSyncs, DateTime.Now);
                return false;
            }
        }
        catch (Exception ex) 
        {
            _logger.LogError("WebConnectionHandler encountered an unexpected error during sync: {Message}", ex.Message);
            SyncStatusMessage = "Sync Failed: Error";
            return false;
        }
    }

    private async Task<bool> SyncGameSession(Guid gameID)
    {
        try
        {
            string sessionJSON = string.Empty;
            if (CurrentTracker == null || CurrentTracker.GameID != gameID)
            {
                var savedTracker = GetSavedStatTracker(gameID);
                if (savedTracker == null)
                {
                    _logger.LogError("Failed to load Stat Tracker for {game}.", gameID);
                    return false;
                }
                sessionJSON = await savedTracker.JSONFromGameSession();
            }
            else
            {
                _logger.LogInformation("Statistics are being synced for an unsaved game session: {gameId}", gameID);
                sessionJSON = await CurrentTracker.JSONFromGameSession();
            }

            bool synced = await _connectionHandler.PostGameSession(sessionJSON);
            if (synced)
            {
                _logger.LogInformation("Successfully posted game session stats for {gameId}", gameID);
            }
            else
                _logger.LogWarning("Failed to post game session stats for {gameId}", gameID);

            return synced;
        }
        catch (Exception ex)
        {
            _logger.LogError("There was an unexpected error when attempting to sync game {id}: {message}", gameID, ex.Message);
            return false;
        }


    }

    private IStatTracker? GetSavedStatTracker(Guid gameID) 
    {
        if (!_gameStats.TryGetValue(gameID, out var metaData))
        {
            _logger.LogWarning("Stat Repo attempted to load saved Stats data from game {id}, but no path was found.", gameID);
            return null;
        }

        if (metaData.SavePath == null || metaData.StreamPosition == null)
        {
            _logger.LogWarning("Stat Repo attempted to load saved Stats data from game {id}, but either no valid path or no stream position was found.", gameID);
            return null;
        }

        try
        {
            IStatTracker newTracker = _statTrackerFactory();
            BinarySerializer.Load([newTracker], metaData.SavePath, (long)metaData.StreamPosition, out _);
            return newTracker;
        }
        catch (Exception ex)
        {
            _logger.LogError("There was an unexpected error as Stat Repo attempted to fetch saved Stat data for game {id}: {message}", gameID, ex.Message);
            return null;
        }
    }

    /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>
    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(async () =>
        {
            List<SerializedData> saveData = [];
            saveData.Add(new(typeof(int), _pendingSyncs));
            saveData.Add(new(typeof(int), _gameStats.Count));
            foreach (var keypair in _gameStats)
            {
                saveData.Add(new(typeof(string), keypair.Key.ToString()));
                saveData.AddRange(await keypair.Value.GetBinarySerials());
            }
            return saveData.ToArray();
        });
    }

    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete;
        try
        {
            _pendingSyncs = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            int numGameStats = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            List<(Guid, SavedStatMetadata)> loadedEntries = [];
            for (int i = 0; i < numGameStats; i++)
            {
                Guid newID = Guid.Parse((string)BinarySerializer.ReadConvertible(reader, typeof(string)));
                var newMetaData = new SavedStatMetadata(_loggerFactory.CreateLogger<SavedStatMetadata>());
                newMetaData.LoadFromBinary(reader);
                loadedEntries.Add((newID, newMetaData));
            }
            _gameStats = loadedEntries.ToDictionary(t => t.Item1, t => t.Item2);

            loadComplete = true;
        }
        catch (Exception ex)
        {
            _logger.LogError("{StatRepo} encountered an unexpected error during deserialization: {Message}.", this, ex.Message);
            loadComplete = false;
        }

        return loadComplete;
    }

    /// <summary>
    /// Saves the current Repo to <see cref="StatFilePath"/>.
    /// </summary>
    public async Task Save()
    {
        bool newStatFile = !File.Exists(StatFilePath);
        try
        {
            await BinarySerializer.Save([this], StatFilePath, newStatFile);
        }
        catch (Exception ex)
        {
            _logger.LogError("{StatRepo} encountered an unexpected error during deserialization: {Message}.", this, ex.Message);
            throw;
        }
    }

    /// <inheritdoc cref="IStatRepo.Load"/>
    public bool Load()
    {
        if (!File.Exists(StatFilePath))
        {
            _logger.LogError("The Statistics File Path provided for loading StatRepo was invalid: {StatFilePath}.", StatFilePath);
            return false;
        }

        using BinaryReader newReader = new(new FileStream(StatFilePath, FileMode.Open, FileAccess.Read));
        if (LoadFromBinary(newReader))
        {
            _pendingSyncs = _gameStats.Values
                .Count(stat => stat is SavedStatMetadata metadata && metadata.SyncPending);
            return true;
        }
        else
            return false;
    }
}
