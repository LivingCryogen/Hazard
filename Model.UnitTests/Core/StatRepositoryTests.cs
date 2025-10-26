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

namespace Model.Tests.Core;
[TestClass]
public class StatRepositoryTests
{
    [TestInitialize]
    public void Setup()
    {
    }

    [TestMethod]
    public async Task Repo_SerializeRoundTrip_Match()
    {
        MockStatTracker testTracker = new(new MockGame());
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
        MockStatRepo testRepo = new(new MockStatTracker(new MockGame()));
        var finalized = await testRepo.FinalizeCurrentGame();

        Assert.IsTrue(finalized);

    }
}
