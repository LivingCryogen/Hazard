using Model.Stats;
using Model.Tests.Core.Mocks;
using Model.Tests.Fixtures;
using Model.Tests.Fixtures.Mocks;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Model.Tests.Core;
[TestClass]
public class StatRepositoryTests
{
    private readonly Guid _testInstallId = Guid.NewGuid();

    [TestInitialize]
    public void Setup()
    {
    }

    [TestMethod]
    public async Task Repo_SerializeRoundTrip_Match()
    {
        MockStatTracker testTracker = new(new MockGame(_testInstallId), _testInstallId);
        MockStatRepo repoToSerialize = new(testTracker);

        string tempStatPath = FileProcessor.GetTempFile();
        var saveResult = await BinarySerializer.Save([testTracker], tempStatPath, true);

        Assert.IsNotNull(saveResult);
        Assert.IsNotEmpty(saveResult);

        bool updated = await repoToSerialize.Update(tempStatPath, saveResult);

        Assert.IsTrue(updated);

        string repoPath = repoToSerialize.StatFilePath;

        await repoToSerialize.Save();

        MockStatRepo repoToDeserialize = new()
        {
            StatFilePath = repoPath
        };

        bool loadResult = repoToDeserialize.Load();

        Assert.IsTrue(loadResult);

        Assert.AreEqual(repoToSerialize.GameStats.Count, repoToDeserialize.GameStats.Count);
        foreach(var kvp in repoToSerialize.GameStats)
        {
            Assert.IsTrue(repoToDeserialize.GameStats.ContainsKey(kvp.Key));
            var deserializedMetadata = repoToDeserialize.GameStats[kvp.Key];
            Assert.AreEqual(kvp.Value.SavePath, deserializedMetadata.SavePath);
            Assert.AreEqual(kvp.Value.ActionCount, deserializedMetadata.ActionCount);
            Assert.AreEqual(kvp.Value.StreamPosition, deserializedMetadata.StreamPosition);
            Assert.AreEqual(kvp.Value.SyncPending, deserializedMetadata.SyncPending);
        }
    }

    [TestMethod]
    public async Task Repo_Finalize_TrackerFinalized()
    {
        MockGame testGame = new();
        Guid testGameID = testGame.ID;
        Guid testInstallID = new();
        MockStatRepo testRepo = new(new MockStatTracker(testGame, testInstallID));
        var testJSON = await testGame.StatTracker.JSONFromGameSession();

        Assert.IsNotNull(testRepo.CurrentTracker);
        Assert.IsNotNull(testRepo.StatFilePath);
        int testActionCount = testRepo.CurrentTracker.TrackedActions;
        string? testDirectory = Path.GetDirectoryName(testRepo.StatFilePath);
        Assert.IsNotNull(testDirectory);
        string expectedFinalPath = Path.Combine(
            testDirectory,
            "completegame" + testGameID.ToString() + ".stat");

        var finalized = await testRepo.FinalizeCurrentGame();

        Assert.IsTrue(finalized);
        Assert.IsNull(testRepo.CurrentTracker);
        Assert.IsNotNull(testRepo.GameStats);
        Assert.AreEqual(1, testRepo.GameStats.Count);
        var finalizedStatPair = testRepo.GameStats.First();
        Assert.AreEqual(testGameID, finalizedStatPair.Key);
        SavedStatMetadata finalizedMetaData = finalizedStatPair.Value;
        Assert.AreEqual(testActionCount, finalizedMetaData.ActionCount);
        Assert.AreEqual(0, finalizedMetaData.StreamPosition);
        Assert.IsTrue(finalizedMetaData.SyncPending);
        Assert.AreEqual(expectedFinalPath, finalizedMetaData.SavePath);
        Assert.IsTrue(File.Exists(expectedFinalPath));

        // Load finalized file and verify contents
        MockStatTracker verifyTracker = new();
        bool loadedTracker = BinarySerializer.Load([verifyTracker], expectedFinalPath, 0, out _);
        Assert.IsTrue(loadedTracker);
        Assert.AreEqual(testGameID, verifyTracker.GameID);
        Assert.AreEqual(testActionCount, verifyTracker.TrackedActions);
        Assert.IsTrue(verifyTracker.Completed);
        Assert.IsNotNull(verifyTracker.CurrentSession);

        var verifyJSON = await verifyTracker.JSONFromGameSession();
        Assert.AreEqual(testJSON, verifyJSON);
    }
}
