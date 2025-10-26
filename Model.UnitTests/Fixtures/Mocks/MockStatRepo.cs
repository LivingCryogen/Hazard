using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Testing.Platform.Logging;
using Model.Stats;
using Model.Stats.Services;
using Model.Tests.Core.Mocks;
using Model.Tests.Fixtures.Stubs;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Tests.Fixtures.Mocks;

public class MockStatRepo : IStatRepo, IBinarySerializable
{
    private readonly string StatsFolderName = string.Empty;
    private readonly Microsoft.Extensions.Logging.ILogger<MockStatRepo> _logger = new LoggerStubT<MockStatRepo>();

    private int _pendingSyncs = 0;

    public MockStatRepo()
    {
        StatFilePath = FileProcessor.GetTempFile();
        StatsFolderName = Path.GetDirectoryName(StatFilePath) ?? "/";
    }

    public MockStatRepo(MockStatTracker tracker)
    {
        StatFilePath = FileProcessor.GetTempFile();
        Tracker = tracker;
        StatsFolderName = Path.GetDirectoryName(StatFilePath) ?? "/";
    }

    public MockStatTracker? Tracker { get; set; }
    public string StatFilePath { get; set; } = string.Empty;

    public IStatTracker? CurrentTracker
    {
        get { return Tracker; }
        set { Tracker = (MockStatTracker?)value; }
    }

    public Dictionary<Guid, SavedStatMetadata> GameStats { get; set; } = [];

    public bool SyncPending => _pendingSyncs > 0;

    public string SyncStatusMessage => throw new NotImplementedException();

    public async Task<bool> Update(string lastSavePath, (string, long)[] objNamesAndPositions)
    {
        try
        {
            if (CurrentTracker == null)
                return false;

            Guid gameID = CurrentTracker.GameID;

            // parse Save result for the streamPosition of StatTracker
            var statTrackerResult = objNamesAndPositions.Where(np => np.Item1 == nameof(MockStatTracker)).FirstOrDefault();
            if (statTrackerResult == default)
            {
                _logger.LogWarning("StatTracker not found in save results");
                return false;
            }
            long trackerPosition = statTrackerResult.Item2;

            if (string.IsNullOrEmpty(lastSavePath))
                return false;

            // If not already tracked, add it
            bool newAdd = GameStats.TryAdd(gameID, new(new LoggerStubT<SavedStatMetadata>())
            {
                SavePath = lastSavePath,
                ActionCount = CurrentTracker.TrackedActions,
                StreamPosition = trackerPosition,
                SyncPending = true
            });

            if (!newAdd) // if it is already tracked, check to see if the Current Tracker is more up to date (contains more Actions)
            {
                if (GameStats.TryGetValue(gameID, out var value)
                        && value is SavedStatMetadata oldData
                        && oldData.ActionCount < CurrentTracker.TrackedActions)
                {
                    var updatedData = new SavedStatMetadata(new LoggerStubT<SavedStatMetadata>())
                    {
                        SavePath = lastSavePath,
                        ActionCount = CurrentTracker.TrackedActions,
                        StreamPosition = trackerPosition,
                        SyncPending = true
                    };
                    GameStats[gameID] = updatedData;
                    _pendingSyncs++;
                }
            }
            else
                _pendingSyncs++;

            await Save();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("There was an unexpected error when attempting to update Stat Repo: {Message}", ex.Message);
            return false;
        }
    }

    private string GetCompletedStatPath(Guid gameID)
    {
        return Path.Combine(StatsFolderName, "completegame" + gameID.ToString() + ".stat");
    }

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

        var completedMetaData = new SavedStatMetadata(new LoggerStubT<SavedStatMetadata>())
        {
            SavePath = completedPath,
            StreamPosition = 0,
            ActionCount = CurrentTracker.TrackedActions,
            SyncPending = true
        };

        // Try to find previous metadata for this game
        if (GameStats.TryGetValue(CurrentTracker.GameID, out var prevMetaData))
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

        GameStats[CurrentTracker.GameID] = completedMetaData;

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

    public bool Load()
    {
        if (!File.Exists(StatFilePath))
        {
            _logger.LogError("The Statistics File Path provided for loading StatRepo was invalid: {StatFilePath}.", StatFilePath);
            return false;
        }

        using BinaryReader newReader = new(new FileStream(StatFilePath, FileMode.Open, FileAccess.Read));
        return LoadFromBinary(newReader);
    }

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
                var newMetaData = new SavedStatMetadata(new LoggerStubT<SavedStatMetadata>());
                newMetaData.LoadFromBinary(reader);
                loadedEntries.Add((newID, newMetaData));
            }
            GameStats = loadedEntries.ToDictionary(t => t.Item1, t => t.Item2);

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

    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(async () =>
        {
            List<SerializedData> saveData = [];
            saveData.Add(new(typeof(int), _pendingSyncs));
            saveData.Add(new(typeof(int), GameStats.Count));
            foreach (var keypair in GameStats)
            {
                saveData.Add(new(typeof(string), keypair.Key.ToString()));
                saveData.AddRange(await keypair.Value.GetBinarySerials());
            }
            return saveData.ToArray();
        });
    }

    public Task<bool> SyncToAzureDB()
    {
        throw new NotImplementedException();
    }
}
