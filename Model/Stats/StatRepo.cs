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

public class StatRepo(WebConnectionHandler connectionHandler, Func<int, IGame> gameFactory, IOptions<AppConfig> options, ILogger<StatRepo> logger) : IBinarySerializable, IStatRepo
{
    private readonly ILogger<StatRepo> _logger = logger;
    private readonly Func<int, IGame> _gameFactory = gameFactory; // for deserialzing old Games and fetching their StatTrackers
    private readonly string StatFilePath = options.Value.StatRepoFilePath;
    private readonly Dictionary<Guid, string> _gamePaths = [];
    private readonly Dictionary<Guid, int> _gameActionCounts = [];
    private readonly HashSet<Guid> _gamesToUpdate = [];
    private readonly WebConnectionHandler _connectionHandler = connectionHandler;

    public IStatTracker? CurrentTracker { get; set; }
    public string SyncStatusMessage { get; private set; } = "Sync";
    /// <summary>
    /// Gets a flag indicating whether there are any syncs (game statistics updates) pending.
    /// </summary>
    public bool SyncPending { get => _gamesToUpdate.Count > 0; }

    public async Task<string?> Update()
    {
        try
        {
            if (CurrentTracker == null)
                return null;

            Guid gameID = CurrentTracker.GameID;
            int trackerNumber = CurrentTracker.TrackedActions;
            string? path = CurrentTracker.LastSavePath;

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
                _gamePaths.TryAdd(gameID, path);
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
            if (CurrentTracker == null || CurrentTracker.GameID != gameID)
            {
                var savedTracker = GetSavedStatTracker(gameID) ?? throw new InvalidDataException(gameID.ToString());
                if (savedTracker == null)
                {
                    _logger.LogError("Failed to load Stat Tracker for {game}.", gameID);
                    return false;
                }
                sessionJSON = await savedTracker.JSONFromGameSession();
            }
            else
                sessionJSON = await CurrentTracker.JSONFromGameSession();

            //2. TODO: HTTP POST TO PROXY
            //3. TODO: HANDLE RESPONSE
            //4. TODO: RETURN PASS/FAIL


        }
        catch (Exception ex)
        {
            _logger.LogError("There was an unexpected error when attempting to sync game {id}: {message}", gameID, ex.Message);
            return false;
        }


    }

    private IStatTracker? GetSavedStatTracker(Guid gameID) /// TODO!! : FIX STARTING POINT FOR FILESTREAM - normal saves have values from View and ViewModel first in the stream, currently this doesn't compensate!!
    {
        IGame savedGame = _gameFactory(0);

        if (!_gamePaths.TryGetValue(gameID, out var path))
        {
            _logger.LogWarning("Stat Repo attempted to load saved Stats data from game {id}, but no path was found.", gameID);
            return null;
        }

        try
        {
            BinarySerializer.Load([savedGame], path);
            return savedGame.StatTracker;
        }
        catch (Exception ex)
        {
            _logger.LogError("There was an unexpected error as Stat Repo attempted to fetch saved Stat data for game {id}: {message}", gameID, ex.Message);
            return null;
        }
    }

    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(() =>
        {
            List<SerializedData> saveData = [];
            saveData.Add(new(typeof(int), _gamePaths.Count));
            foreach (var keypair in _gamePaths)
            {
                saveData.Add(new(typeof(string), keypair.Key.ToString()));
                saveData.Add(new(typeof(string), keypair.Value));
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
            int numGamePathPairs = (int)BinarySerializer.ReadConvertible(newReader, typeof(int));
            for (int i = 0; i < numGamePathPairs; i++)
                _gamePaths.Add(Guid.Parse((string)BinarySerializer.ReadConvertible(newReader, typeof(string))),
                    (string)BinarySerializer.ReadConvertible(newReader, typeof(string)));
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
