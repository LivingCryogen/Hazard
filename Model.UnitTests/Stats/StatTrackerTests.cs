using Model.Tests.Core.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Tests.Stats;

[TestClass]
public class StatTrackerTests
{
    private MockGame _testGame;
    private MockStatTracker _testTracker;

    [TestInitialize]
    public void Setup()
    {
        _testGame = new MockGame();
        _testTracker = (MockStatTracker)_testGame.StatTracker;
    }

    [TestMethod]
    public void MockPlayer1_Attacks_StatsRecorded()
    {
        // _testGame.Regulator.Battle()
    }
}
