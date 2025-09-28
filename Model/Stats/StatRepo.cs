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

public class StatRepo(WebConnectionHandler connectionHandler, Func<IStatTracker> statFactory, IOptions<AppConfig> options, ILogger<StatRepo> logger) : IBinarySerializable, IStatRepo
{
    private readonly ILogger<StatRepo> _logger = logger;
    private readonly string StatFilePath = options.Value.StatRepoFilePath;
    private readonly Dictionary<Guid, (string, long)> _gameStatLocs = [];
    private readonly Dictionary<Guid, int> _gameActionCounts = [];
    private readonly HashSet<Guid> _gamesToUpdate = [];
    private readonly WebConnectionHandler _connectionHandler = connectionHandler;
    private readonly Func<IStatTracker> _statTrackerFactory = statFactory;

    /// <inheritdoc cref="IStatRepo.CurrentTracker" />
    public IStatTracker? CurrentTracker { get; set; }
    /// <inheritdoc cref="IStatRepo.SyncPending"/>
    public string SyncStatusMessage { get; private set; } = "Sync";
    /// <summary>
    /// Gets a flag indicating whether there are any syncs (game statistics updates) pending.
    /// </summary>
    public bool SyncPending { get => _gamesToUpdate.Count > 0; }

    /// <inheritdoc cref="IStatRepo.Update(ValueTuple{string, long}[])"/>
    public async Task<string?> Update((string, long)[] objNamesAndPositions)
    {
        try
        {
            if (CurrentTracker == null)
                return null;

            Guid gameID = CurrentTracker.GameID;
            int trackerNumber = CurrentTracker.TrackedActions;
            string? path = CurrentTracker.LastSavePath;

            // parse Save result for the streamPosition of StatTracker
            var statTrackerResult = objNamesAndPositions.Where(np => np.Item1 == nameof(StatTracker)).FirstOrDefault();
            if (statTrackerResult == default)
            {
                _logger.LogWarning("StatTracker not found in save results");
                return null;
            }
            long trackerPosition = statTrackerResult.Item2;

            if (string.IsNullOrEmpty(path))
                return null;

            bool needsUpdate = false;
            if (!_gameActionCounts.TryAdd(gameID, trackerNumber)) // If not already tracked, add it
            {
                if (_gameActionCounts.TryGetValue(gameID, out var value) // If it is tracked, compare number of tracked stats to determine whether an update is needed
                    && value is int oldNumber
                    && trackerNumber > oldNumber)
                    needsUpdate = true;
            }
            else
                needsUpdate = true;

            if (!needsUpdate)
                return null;


            _gamesToUpdate.Add(gameID);

            // Only update the path mapping if there is a path to map and the game is further ahead;
            // (If a game is saved to multiple files, we only care about the one most recently updated)
            if (path != null)
            {
                _gameStatLocs.TryAdd(gameID, (path, trackerPosition));
            }

            await Save();
            return path;
        }
        catch (Exception ex)
        {
            _logger.LogError("There was an unexpected error when attempting to update Stat Repo: {Message}", ex.Message);
            return null;
        }   
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
            int syncsNeeded = _gamesToUpdate.Count;
            bool allSessionsSynced = true;
            List<Guid> failedSyncs = [];

            foreach (Guid gameID in _gamesToUpdate)
            {
                SyncStatusMessage = $"Syncing game {gameID:N}...";

                bool synced = await SyncGameSession(gameID);

                if (synced)
                {
                    _logger.LogInformation("Succesfully synced game {ID}", gameID);
                    _gamesToUpdate.Remove(gameID);
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
                _logger.LogInformation("Sync completed succesfully on {count} games at {time}", syncsNeeded, DateTime.Now);
                return true;
            }
            else
            {
                SyncStatusMessage = $"Sync Succeeded for {syncsNeeded - failedSyncs.Count} games; Failed for {failedSyncs.Count}.";
                _logger.LogInformation("Sync failed for {failednum} of {total} games at {time}.", failedSyncs.Count, syncsNeeded, DateTime.Now);
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
            int trackedActions;
            if (CurrentTracker == null || CurrentTracker.GameID != gameID)
            {
                var savedTracker = GetSavedStatTracker(gameID);
                if (savedTracker == null)
                {
                    _logger.LogError("Failed to load Stat Tracker for {game}.", gameID);
                    return false;
                }
                sessionJSON = await savedTracker.JSONFromGameSession();
                trackedActions = savedTracker.TrackedActions;
            }
            else
            {
                sessionJSON = await CurrentTracker.JSONFromGameSession();
                trackedActions = CurrentTracker.TrackedActions;
            }

            bool synced = await _connectionHandler.PostGameSession(sessionJSON, trackedActions);
            if (synced)
                _logger.LogInformation("Successfully posted game session stats for {gameId}", gameID);
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
        if (!_gameStatLocs.TryGetValue(gameID, out var pathLoc))
        {
            _logger.LogWarning("Stat Repo attempted to load saved Stats data from game {id}, but no path was found.", gameID);
            return null;
        }

        try
        {
            IStatTracker newTracker = _statTrackerFactory();
            BinarySerializer.Load([newTracker], pathLoc.Item1, pathLoc.Item2, out _);
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
        return await Task.Run(() =>
        {
            List<SerializedData> saveData = [];
            saveData.Add(new(typeof(int), _gameStatLocs.Count));
            foreach (var keypair in _gameStatLocs)
            {
                saveData.Add(new(typeof(string), keypair.Key.ToString()));
                saveData.Add(new(typeof(string), keypair.Value.Item1));
                saveData.Add(new(typeof(long), keypair.Value.Item2));
            }
            saveData.Add(new(typeof(int), _gameActionCounts.Count));
            foreach (var keypair in _gameActionCounts)
            {
                saveData.Add(new(typeof(string), keypair.Key.ToString()));
                saveData.Add(new(typeof(int), keypair.Value));
            }
            saveData.Add(new(typeof(int), _gamesToUpdate.Count));
            foreach (Guid gameID in _gamesToUpdate)
                saveData.Add(new(typeof(string), gameID.ToString()));
            return saveData.ToArray();
        });
    }

    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
    public bool LoadFromBinary(BinaryReader reader)
    {
        if (!File.Exists(StatFilePath))
        {
            _logger.LogError("{StatRepo} failed to load from {path}.", this, StatFilePath);
            return false;
        }

        using BinaryReader newReader = new(new FileStream(StatFilePath, FileMode.Open, FileAccess.Read));

        bool loadComplete;
        try
        {
            int numGameLocPairs = (int)BinarySerializer.ReadConvertible(newReader, typeof(int));
            for (int i = 0; i < numGameLocPairs; i++)
                _gameStatLocs.Add(Guid.Parse((string)BinarySerializer.ReadConvertible(newReader, typeof(string))),
                    ((string)BinarySerializer.ReadConvertible(newReader, typeof(string)),
                    (long)BinarySerializer.ReadConvertible(newReader, typeof(long))));
            int numGameTrackNumPairs = (int)BinarySerializer.ReadConvertible(newReader, typeof(int));
            for (int i = 0; i < numGameTrackNumPairs; i++)
                _gameActionCounts.Add(Guid.Parse((string)BinarySerializer.ReadConvertible(newReader, typeof(string))),
                    (int)BinarySerializer.ReadConvertible(newReader, typeof(int)));
            int numGamesToUpdate = (int)BinarySerializer.ReadConvertible(newReader, typeof(int));
            for (int i = 0; i < numGamesToUpdate; i++)
                _gamesToUpdate.Add(Guid.Parse((string)BinarySerializer.ReadConvertible(newReader, typeof(string))));
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
}
