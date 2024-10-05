using Hazard_Model.Core;
using Hazard_Model.DataAccess;
using Hazard_Model.Tests.Core.Mocks;
using Hazard_Model.Tests.Entities.Mocks;
using Hazard_Model.Tests.Fixtures;
using Hazard_Model.Tests.Fixtures.Stubs;
using Hazard_Share.Enums;
using Hazard_Share.Interfaces.Model;
using Hazard_Share.Services.Serializer;

namespace Hazard_Model.Tests.DataAccess;

[TestClass]
public class BinarySerializerTests
{
    private readonly MockGame _toSerialGame = new();
    private readonly MockGame _deserialGame = new();
    private string _testFileName;

    public BinarySerializerTests()
    {
        BinarySerializer.InitializeLogger(new LoggerStub());
    }

    [TestInitialize]
    public void Setup()
    {
        _testFileName = FileProcessor.GetTempFile();
        _deserialGame.Wipe();
    }

    [TestMethod]
    public async Task Board_RoundTrip_Match()
    {
        await BinarySerializer.Save([_toSerialGame.Board], _testFileName, true);
        if (BinarySerializer.Load([_deserialGame.Board], _testFileName)) {
            Assert.IsNotNull(_toSerialGame.Board);
            Assert.IsNotNull(_deserialGame.Board);
            Assert.AreEqual(_toSerialGame.Board.Geography.NumTerritories, _deserialGame.Board.Geography.NumTerritories);
            Assert.AreEqual(_toSerialGame.Board.Geography.NumContinents, _deserialGame.Board.Geography.NumContinents);
            foreach (var contKey in _toSerialGame.Board.ContinentOwner.Keys)
                Assert.AreEqual(_toSerialGame.Board.ContinentOwner[contKey], _deserialGame.Board.ContinentOwner[contKey]);
            foreach (var terrKey in _toSerialGame.Board.TerritoryOwner.Keys) {
                Assert.AreEqual(_toSerialGame.Board.TerritoryOwner[terrKey], _deserialGame.Board.TerritoryOwner[terrKey]);
                Assert.AreEqual(_toSerialGame.Board.Armies[terrKey], _deserialGame.Board.Armies[terrKey]);
            }
        }
        else { Assert.Fail(); }
    }

    [TestMethod]
    public async Task Players_RoundTrip_Match()
    {
        _toSerialGame.Players.Clear();
        _toSerialGame.Players.Add(new MockPlayer(0, 6, _toSerialGame.Cards.CardFactory, _toSerialGame.Values, _toSerialGame.Board, new LoggerStubT<MockPlayer>()) {
            Name = "TestPlayer1",
            ArmyPool = 50,
            ControlledTerritories = [TerrID.Alaska, TerrID.Kamchatka, TerrID.Peru],
            Hand = [new MockCard()],
        });
        _toSerialGame.Players.Add(new MockPlayer(1, 6, _toSerialGame.Cards.CardFactory, _toSerialGame.Values, _toSerialGame.Board, new LoggerStubT<MockPlayer>()) {
            Name = "TestPlayer2",
            ArmyPool = 40,
            ControlledTerritories = [TerrID.EastAfrica, TerrID.Afghanistan, TerrID.Argentina],
            Hand = [new MockCard(), new MockCard()],
        });
        _toSerialGame.Players.Add(new MockPlayer(2, 6, _toSerialGame.Cards.CardFactory, _toSerialGame.Values, _toSerialGame.Board, new LoggerStubT<MockPlayer>()) {
            Name = "TestPlayer3",
            ArmyPool = 30,
            ControlledTerritories = [TerrID.Siberia],
            Hand = [],
        });
        _toSerialGame.Players.Add(new MockPlayer(3, 6, _toSerialGame.Cards.CardFactory, _toSerialGame.Values, _toSerialGame.Board, new LoggerStubT<MockPlayer>()) {
            Name = "TestPlayer4",
            ArmyPool = 20,
            ControlledTerritories = [],
            Hand = [new MockCard(), new MockCard(), new MockCard()],
        });
        _toSerialGame.Players.Add(new MockPlayer(4, 6, _toSerialGame.Cards.CardFactory, _toSerialGame.Values, _toSerialGame.Board, new LoggerStubT<MockPlayer>()) {
            Name = "TestPlayer5",
            ArmyPool = 10,
            ControlledTerritories = [TerrID.Ural, TerrID.Japan, TerrID.Brazil],
            Hand = [new MockCard(), new MockCard()],
        });
        _toSerialGame.Players.Add(new MockPlayer(5, 6, _toSerialGame.Cards.CardFactory, _toSerialGame.Values, _toSerialGame.Board, new LoggerStubT<MockPlayer>()) {
            Name = "TestPlayer6",
            ArmyPool = 0,
            ControlledTerritories = [TerrID.CentralAmerica, TerrID.China, TerrID.Congo, TerrID.Ontario, TerrID.Quebec],
            Hand = [new MockCard(), new MockCard(), new MockCard(), new MockCard()],
        });
        _deserialGame.Players.Clear();
        ((MockCardBase)_deserialGame.Cards).Wipe();

        for (int i = 0; i < _toSerialGame.Players.Count; i++) {
            await BinarySerializer.Save([_toSerialGame.Players[i]], _testFileName, true);
            _deserialGame.Players.Add(new MockPlayer(0, 0, _deserialGame.Cards.CardFactory, _deserialGame.Values, _deserialGame.Board, new LoggerStubT<MockPlayer>()));
            if (BinarySerializer.Load([_deserialGame.Players[i]], _testFileName)) {
                Assert.AreEqual(_toSerialGame.Players[i].Number, _deserialGame.Players[i].Number);
                Assert.AreEqual(_toSerialGame.Players[i].Name, _deserialGame.Players[i].Name);
                Assert.AreEqual(_toSerialGame.Players[i].HasCardSet, _deserialGame.Players[i].HasCardSet);
                Assert.AreEqual(_toSerialGame.Players[i].ArmyPool, _deserialGame.Players[i].ArmyPool);
                Assert.AreEqual(_toSerialGame.Players[i].ContinentBonus, _deserialGame.Players[i].ContinentBonus);
                Assert.AreEqual(_toSerialGame.Players[i].ControlledTerritories.Count, _deserialGame.Players[i].ControlledTerritories.Count);
                for (int j = 0; j < _toSerialGame.Players[i].ControlledTerritories.Count; j++) {
                    Assert.AreEqual(_toSerialGame.Players[i].ControlledTerritories[j], _deserialGame.Players[i].ControlledTerritories[j]);
                }
                Assert.AreEqual(_toSerialGame.Players[i].Hand.Count, _deserialGame.Players[i].Hand.Count);
                for (int j = 0; j < _toSerialGame.Players[i].Hand.Count; j++) {
                    Assert.AreEqual(_toSerialGame.Players[i].Hand[j].ParentTypeName, _deserialGame.Players[i].Hand[j].ParentTypeName);
                    Assert.AreEqual(_toSerialGame.Players[i].Hand[j].Target.Length, _deserialGame.Players[i].Hand[j].Target.Length);
                    for (int k = 0; k < _toSerialGame.Players[i].Hand[j].Target.Length; k++) {
                        Assert.AreEqual(_toSerialGame.Players[i].Hand[j].Target[k], _deserialGame.Players[i].Hand[j].Target[k]);
                        Assert.AreEqual(((ITroopCard)(_toSerialGame.Players[i].Hand[j])).Insigne, ((ITroopCard)(_deserialGame.Players[i].Hand[j])).Insigne);
                        Assert.IsNull(_toSerialGame.Players[i].Hand[j].CardSet);
                        Assert.AreEqual(_toSerialGame.Players[i].Hand[j].CardSet, _deserialGame.Players[i].Hand[j].CardSet); // two-step initialization means cardset isn't initialized until after LoadCardBase
                        Assert.AreEqual(_toSerialGame.Players[i].Hand[j].IsTradeable, _deserialGame.Players[i].Hand[j].IsTradeable);
                        Assert.AreEqual(_toSerialGame.Players[i].Hand[j].IsTradeable, _deserialGame.Players[i].Hand[j].IsTradeable);
                        Assert.IsInstanceOfType(_toSerialGame.Players[i].Hand[j], typeof(MockCard));
                        Assert.IsInstanceOfType(_deserialGame.Players[i].Hand[j], typeof(MockCard));
                        MockCard serialCard = (MockCard)_toSerialGame.Players[i].Hand[j];
                        MockCard deserialCard = (MockCard)_toSerialGame.Players[i].Hand[j];
                        Assert.AreEqual(serialCard.TestBytes, deserialCard.TestBytes);
                        Assert.AreEqual(serialCard.TestInts, deserialCard.TestInts);
                        Assert.AreEqual(serialCard.TestLongs, deserialCard.TestLongs);
                        Assert.AreEqual(serialCard.TestBools, deserialCard.TestBools);
                        Assert.AreEqual(serialCard.TestStrings, deserialCard.TestStrings);
                    }
                }
            }
            else { Assert.Fail(); }
        }
        Assert.AreEqual(_toSerialGame.Players.Count, _deserialGame.Players.Count);
    }

    [TestMethod]
    public async Task CardBase_RoundTrip_Match()
    {
        await BinarySerializer.Save([_toSerialGame.Cards], _testFileName, true);

        if (BinarySerializer.Load([_deserialGame.Cards], _testFileName)) {
            Assert.IsNotNull(_toSerialGame.Cards.Sets);
            Assert.IsNotNull(_deserialGame.Cards.Sets);
            Assert.AreEqual(_toSerialGame.Cards.Sets.Count, _deserialGame.Cards.Sets.Count);
            for (int i = 0; i < _toSerialGame.Cards.Sets.Count; i++) {
                Assert.AreEqual(_toSerialGame.Cards.Sets[i].Name, _deserialGame!.Cards.Sets[i].Name);
                Assert.AreEqual(_toSerialGame.Cards.Sets[i].MemberTypeName, _deserialGame!.Cards.Sets[i].MemberTypeName);
                Assert.IsNotNull(_toSerialGame.Cards.Sets[i].Cards);
                Assert.IsNotNull(_deserialGame.Cards.Sets[i].Cards);
                Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards.Count, _deserialGame.Cards.Sets[i].Cards.Count);
                for (int j = 0; j < _toSerialGame.Cards.Sets[i].Cards.Count; j++) {
                    Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards[j].ParentTypeName, _deserialGame!.Cards.Sets[i].Cards![j].ParentTypeName);
                    Assert.IsNotNull(_toSerialGame.Cards.Sets[i].Cards[j].PropertySerializableTypeMap);
                    Assert.IsNotNull(_deserialGame.Cards.Sets[i].Cards[j].PropertySerializableTypeMap);
                    Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards[j].PropertySerializableTypeMap.Keys.Count, _deserialGame.Cards.Sets[i].Cards[j].PropertySerializableTypeMap.Keys.Count);
                    Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards[j].IsTradeable, _deserialGame.Cards.Sets[i].Cards[j].IsTradeable);
                    Assert.IsNotNull(_toSerialGame.Cards.Sets[i].Cards[j].CardSet);
                    Assert.IsNotNull(_deserialGame.Cards.Sets[i].Cards[j].CardSet);
                    Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards[j].CardSet.Name, _deserialGame.Cards.Sets[i].Cards[j].CardSet.Name);
                    Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards[j].Target[0], _deserialGame.Cards.Sets[i].Cards[j].Target[0]); // could test the entire array but the default Targets are always length 1
                }
                Assert.AreEqual(_toSerialGame.Cards.Sets[i].ForcesTrade, _deserialGame!.Cards.Sets[i].ForcesTrade);
                Assert.IsNotNull(_toSerialGame.Cards.Sets[i].JData); // "JData" refers to data loaded from .json during new game asset loading
                Assert.IsNull(_deserialGame.Cards.Sets[i].JData); // "JData" refers to data loaded from .json during new game asset loading; when loading from binary, this is not used.
            }
            Assert.IsNotNull(_toSerialGame.Cards.GameDeck);
            Assert.IsNotNull(_deserialGame.Cards.GameDeck);
            Assert.IsNotNull(_toSerialGame.Cards.GameDeck.Library);
            Assert.IsNotNull(_deserialGame.Cards.GameDeck.Library);
            Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library.Count, _deserialGame!.Cards.GameDeck.Library.Count);
            for (int j = 0; j < _toSerialGame.Cards.GameDeck.Library.Count; j++) {
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].ParentTypeName, _deserialGame!.Cards.GameDeck.Library[j].ParentTypeName);
                Assert.IsNotNull(_toSerialGame.Cards.GameDeck.Library[j].PropertySerializableTypeMap);
                Assert.IsNotNull(_deserialGame.Cards.GameDeck.Library[j].PropertySerializableTypeMap);
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].PropertySerializableTypeMap.Keys.Count, _deserialGame.Cards.GameDeck.Library[j].PropertySerializableTypeMap.Keys.Count);
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].IsTradeable, _deserialGame.Cards.GameDeck.Library[j].IsTradeable);
                Assert.IsNotNull(_toSerialGame.Cards.GameDeck.Library[j].CardSet);
                Assert.IsNotNull(_deserialGame.Cards.GameDeck.Library[j].CardSet);
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].CardSet.Name, _deserialGame.Cards.GameDeck.Library[j].CardSet.Name);
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].Target[0], _deserialGame.Cards.GameDeck.Library[j].Target[0]); // could test the entire array but the default Targets are always length 1
            }
            Assert.IsNotNull(_toSerialGame.Cards.GameDeck.DiscardPile);
            Assert.IsNotNull(_deserialGame.Cards.GameDeck.DiscardPile);
            for (int j = 0; j < _toSerialGame.Cards.GameDeck.DiscardPile.Count; j++) {
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].ParentTypeName, _deserialGame!.Cards.GameDeck.DiscardPile[j].ParentTypeName);
                Assert.IsNotNull(_toSerialGame.Cards.GameDeck.DiscardPile[j].PropertySerializableTypeMap);
                Assert.IsNotNull(_deserialGame.Cards.GameDeck.DiscardPile[j].PropertySerializableTypeMap);
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].PropertySerializableTypeMap.Keys.Count, _deserialGame.Cards.GameDeck.DiscardPile[j].PropertySerializableTypeMap.Keys.Count);
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].IsTradeable, _deserialGame.Cards.GameDeck.DiscardPile[j].IsTradeable);
                Assert.IsNotNull(_toSerialGame.Cards.GameDeck.DiscardPile[j].CardSet);
                Assert.IsNotNull(_deserialGame.Cards.GameDeck.DiscardPile[j].CardSet);
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].CardSet.Name, _deserialGame.Cards.GameDeck.DiscardPile[j].CardSet.Name);
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].Target[0], _deserialGame.Cards.GameDeck.DiscardPile[j].Target[0]); // could test the entire array but the default Targets are always length 1
            }
        }
        else Assert.Fail();
    }

    [TestMethod]
    public async Task StateMachine_RoundTrip_Match()
    {
        _toSerialGame.State.NumTrades = 4;
        _toSerialGame.State.CurrentPhase = GamePhase.Attack;
        _toSerialGame.State.Round = 8;
        _toSerialGame.State.PlayerTurn = 1;
        _toSerialGame.State.PhaseStageTwo = true;
        _toSerialGame.State.Winner = 1;

        await BinarySerializer.Save([_toSerialGame.State], _testFileName, true);

        if (BinarySerializer.Load([_deserialGame.State], _testFileName)) {
            Assert.AreEqual(_toSerialGame.State.NumTrades, _deserialGame.State.NumTrades);
            Assert.AreEqual(_toSerialGame.State.CurrentPhase, _deserialGame.State.CurrentPhase);
            Assert.AreEqual(_toSerialGame.State.Round, _deserialGame.State.Round);
            Assert.AreEqual(_toSerialGame.State.PlayerTurn, _deserialGame.State.PlayerTurn);
            Assert.AreEqual(_toSerialGame.State.PhaseStageTwo, _deserialGame.State.PhaseStageTwo);
            Assert.AreEqual(_toSerialGame.State.Winner, _deserialGame.State.Winner);
        } else Assert.Fail();
    }
}
//        var stateSave = _toSerialGame.State.GetSaveData();
//        using BinaryWriter writer = new(_currentStream);
//        BinarySerializer.WriteData(writer, stateSave, _loggerStub);

//        _currentStream.Dispose();
//        _currentStream = null;

//        _currentStream = new(_testFileName, FileMode.Open);
//        _testDeserializer = new(_deserialGame, _currentStream, SharedRegister.Registry, _loggerStub);

//        using BinaryReader reader = new(_currentStream);
//        _testDeserializer.LoadStateMachine(reader);
//        Assert.IsNotNull(_deserialGame.State);
//        Assert.AreEqual(_toSerialGame.State.PhaseStageTwo, _deserialGame.State.PhaseStageTwo);
//        Assert.AreEqual(_toSerialGame.State.CurrentPhase, _deserialGame.State.CurrentPhase);
//        Assert.AreEqual(_toSerialGame.State.PlayerTurn, _deserialGame.State.PlayerTurn);
//        Assert.AreEqual(_toSerialGame.State.Round, _deserialGame.State.Round);
//        Assert.AreEqual(_toSerialGame.State.NumTrades, _deserialGame.State.NumTrades);
//    }
//    [TestMethod]
//    public void Regulator_RoundTrip_Match()
//    {
//        Assert.IsNotNull(_currentStream);
//        Assert.IsNotNull(_testFileName);
//        Assert.IsNotNull(_testSerializer);
//        Assert.IsNotNull(_toSerialGame.Regulator);

//        List<(object? DataObj, Type? SerialType)> saveData = [];
//        BinaryWriter writer = new(_currentStream);

//        BinarySerializer.SerializeRegulator(_toSerialGame.Regulator, saveData);
//        BinarySerializer.WriteData(writer, saveData, _loggerStub);

//        writer.Dispose();
//        _currentStream.Dispose();
//        _currentStream = null;

//        _currentStream = new(_testFileName, FileMode.Open);
//        using BinaryReader reader = new(_currentStream);
//        _testDeserializer = new(_deserialGame, _currentStream, SharedRegister.Registry, _loggerStub);
//        Assert.IsNotNull(_deserialGame.Regulator);
//        _deserialGame.Regulator.Initialize(_deserialGame, _testDeserializer.LoadRegulatorValues(reader));

//        Assert.AreEqual(_toSerialGame.Regulator.CurrentActionsLimit, _deserialGame.Regulator.CurrentActionsLimit);
//        Assert.AreEqual(_toSerialGame.Regulator.PhaseActions, _deserialGame.Regulator.PhaseActions);
//        if (_toSerialGame.Regulator.Reward == null)
//            Assert.IsNull(_deserialGame.Regulator.Reward);
//        else {
//            Assert.IsNotNull(_deserialGame.Regulator.Reward);
//            Assert.AreEqual(_toSerialGame.Regulator.Reward.Target[0], _deserialGame.Regulator.Reward.Target[0]);
//            Assert.AreEqual(((MockCard)_toSerialGame.Regulator.Reward).Insigne, ((MockCard)_deserialGame.Regulator.Reward).Insigne);
//            Assert.AreEqual(_toSerialGame.Regulator.Reward.ParentTypeName, _deserialGame.Regulator.Reward.ParentTypeName);
//            Assert.IsNotNull(_toSerialGame.Regulator.Reward.CardSet);
//            Assert.IsNotNull(_deserialGame.Regulator.Reward.CardSet);
//            Assert.AreEqual(_toSerialGame.Regulator.Reward.IsTradeable, _deserialGame.Regulator.Reward.IsTradeable);
//        }
//    }
//    [TestMethod]
//    public async Task EntireGame_RoundTrip_Match()
//    {
//        Assert.IsNotNull(_currentStream);
//        Assert.IsNotNull(_testFileName);
//        Assert.IsNotNull(_testSerializer);

//        await _testSerializer.SaveGame("");

//        _currentStream.Dispose();
//        _currentStream = null;

//        _currentStream = new(_testFileName, FileMode.Open);
//        _testDeserializer = new(_deserialGame, _currentStream, SharedRegister.Registry, _loggerStub);

//        _testDeserializer.LoadGame();

//        // Game class Specific
//        Assert.AreEqual(_toSerialGame.ID, _deserialGame.ID);

//        // Board
//        Assert.IsNotNull(_toSerialGame.Board);
//        Assert.IsNotNull(_deserialGame.Board);
//        Assert.AreEqual(_toSerialGame.Board.Geography.NumTerritories, _deserialGame.Board.Geography.NumTerritories);
//        Assert.AreEqual(_toSerialGame.Board.Geography.NumContinents, _deserialGame.Board.Geography.NumContinents);
//        Assert.AreEqual(_toSerialGame.Board.Armies[0], _deserialGame.Board.Armies[0]);
//        Assert.AreEqual(_toSerialGame.Board.ContinentOwner[0], _deserialGame.Board.ContinentOwner[0]);

//        // CardBase
//        Assert.IsNotNull(_toSerialGame);
//        Assert.IsNotNull(_toSerialGame.Cards);
//        Assert.IsNotNull(_toSerialGame.Cards.Sets);
//        Assert.IsNotNull(_deserialGame);
//        Assert.IsNotNull(_deserialGame.Cards);
//        Assert.IsNotNull(_deserialGame.Cards.Sets);
//        Assert.AreEqual(_toSerialGame.Cards.Sets.Count, _deserialGame!.Cards.Sets.Count);
//        for (int i = 0; i < _toSerialGame.Cards.Sets.Count; i++) {
//            Assert.AreEqual(_toSerialGame.Cards.Sets[i].Name, _deserialGame!.Cards.Sets[i].Name);
//            Assert.AreEqual(_toSerialGame.Cards.Sets[i].MemberTypeName, _deserialGame!.Cards.Sets[i].MemberTypeName);
//            Assert.IsNotNull(_toSerialGame.Cards.Sets[i].Cards);
//            Assert.IsNotNull(_deserialGame.Cards.Sets[i].Cards);
//            Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards!.Length, _deserialGame.Cards.Sets[i].Cards!.Length);
//            for (int j = 0; j < _toSerialGame.Cards.Sets[i].Cards!.Length; j++) {
//                Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards![j].ParentTypeName, _deserialGame!.Cards.Sets[i].Cards![j].ParentTypeName);
//                Assert.IsNotNull(_toSerialGame.Cards.Sets[i].Cards![j].PropertySerializableTypeMap);
//                Assert.IsNotNull(_deserialGame.Cards.Sets[i].Cards![j].PropertySerializableTypeMap);
//                Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards![j].PropertySerializableTypeMap!.Keys.Count, _deserialGame!.Cards.Sets[i].Cards![j].PropertySerializableTypeMap!.Keys.Count);
//                Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards![j].IsTradeable, _deserialGame!.Cards.Sets[i].Cards![j].IsTradeable);
//                Assert.IsNotNull(_toSerialGame.Cards.Sets[i].Cards![j].CardSet);
//                Assert.IsNotNull(_deserialGame.Cards.Sets[i].Cards![j].CardSet);
//                Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards![j].CardSet!.Name, _deserialGame!.Cards.Sets[i].Cards![j].CardSet!.Name);
//                Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards![j].Target[0], _deserialGame!.Cards.Sets[i].Cards![j].Target[0]); // could test the entire array but the default Targets are always length 1
//            }
//            Assert.AreEqual(_toSerialGame.Cards.Sets[i].ForcesTrade, _deserialGame!.Cards.Sets[i].ForcesTrade);
//            Assert.IsNotNull(_toSerialGame.Cards.Sets[i].JData); // "JData" refers to data loaded from .json during new game asset loading
//            Assert.IsNull(_deserialGame.Cards.Sets[i].JData); // "JData" refers to data loaded from .json during new game asset loading; when loading from binary, this is not used.
//        }
//        Assert.IsNotNull(_toSerialGame.Cards.GameDeck);
//        Assert.IsNotNull(_deserialGame.Cards.GameDeck);
//        Assert.IsNotNull(_toSerialGame.Cards.GameDeck.Library);
//        Assert.IsNotNull(_deserialGame.Cards.GameDeck.Library);
//        Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library.Count, _deserialGame!.Cards.GameDeck.Library.Count);
//        for (int j = 0; j < _toSerialGame.Cards.GameDeck.Library.Count; j++) {
//            Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].ParentTypeName, _deserialGame!.Cards.GameDeck.Library[j].ParentTypeName);
//            Assert.IsNotNull(_toSerialGame.Cards.GameDeck.Library[j].PropertySerializableTypeMap);
//            Assert.IsNotNull(_deserialGame.Cards.GameDeck.Library[j].PropertySerializableTypeMap);
//            Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].PropertySerializableTypeMap!.Keys.Count, _deserialGame!.Cards.GameDeck.Library[j].PropertySerializableTypeMap!.Keys.Count);
//            Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].IsTradeable, _deserialGame!.Cards.GameDeck.Library[j].IsTradeable);
//            Assert.IsNotNull(_toSerialGame.Cards.GameDeck.Library[j].CardSet);
//            Assert.IsNotNull(_deserialGame.Cards.GameDeck.Library[j].CardSet);
//            Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].CardSet!.Name, _deserialGame!.Cards.GameDeck.Library[j].CardSet!.Name);
//            Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].Target[0], _deserialGame!.Cards.GameDeck.Library[j].Target[0]); // could test the entire array but the default Targets are always length 1
//        }
//        Assert.IsNotNull(_toSerialGame.Cards.GameDeck.DiscardPile);
//        Assert.IsNotNull(_deserialGame.Cards.GameDeck.DiscardPile);
//        for (int j = 0; j < _toSerialGame.Cards.GameDeck.DiscardPile.Count; j++) {
//            Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].ParentTypeName, _deserialGame!.Cards.GameDeck.DiscardPile[j].ParentTypeName);
//            Assert.IsNotNull(_toSerialGame.Cards.GameDeck.DiscardPile[j].PropertySerializableTypeMap);
//            Assert.IsNotNull(_deserialGame.Cards.GameDeck.DiscardPile[j].PropertySerializableTypeMap);
//            Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].PropertySerializableTypeMap!.Keys.Count, _deserialGame!.Cards.GameDeck.DiscardPile[j].PropertySerializableTypeMap!.Keys.Count);
//            Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].IsTradeable, _deserialGame!.Cards.GameDeck.DiscardPile[j].IsTradeable);
//            Assert.IsNotNull(_toSerialGame.Cards.GameDeck.DiscardPile[j].CardSet);
//            Assert.IsNotNull(_deserialGame.Cards.GameDeck.DiscardPile[j].CardSet);
//            Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].CardSet!.Name, _deserialGame!.Cards.GameDeck.DiscardPile[j].CardSet!.Name);
//            Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].Target[0], _deserialGame!.Cards.GameDeck.DiscardPile[j].Target[0]); // could test the entire array but the default Targets are always length 1
//        }

//        //State Machine
//        Assert.IsNotNull(_toSerialGame.State);
//        Assert.IsNotNull(_deserialGame.State);
//        Assert.AreEqual(_toSerialGame.State.PhaseStageTwo, _deserialGame.State.PhaseStageTwo);
//        Assert.AreEqual(_toSerialGame.State.CurrentPhase, _deserialGame.State.CurrentPhase);
//        Assert.AreEqual(_toSerialGame.State.PlayerTurn, _deserialGame.State.PlayerTurn);
//        Assert.AreEqual(_toSerialGame.State.Round, _deserialGame.State.Round);
//        Assert.AreEqual(_toSerialGame.State.NumTrades, _deserialGame.State.NumTrades);

//        // Regulator
//        Assert.IsNotNull(_toSerialGame.Regulator);
//        Assert.IsNotNull(_deserialGame.Regulator);
//        Assert.AreEqual(_toSerialGame.Regulator.CurrentActionsLimit, _deserialGame.Regulator.CurrentActionsLimit);
//        Assert.AreEqual(_toSerialGame.Regulator.PhaseActions, _deserialGame.Regulator.PhaseActions);
//        if (_toSerialGame.Regulator.Reward == null)
//            Assert.IsNull(_deserialGame.Regulator.Reward);
//        else {
//            Assert.IsNotNull(_deserialGame.Regulator.Reward);
//            Assert.AreEqual(_toSerialGame.Regulator.Reward.Target[0], _deserialGame.Regulator.Reward.Target[0]);
//            Assert.AreEqual(((MockCard)_toSerialGame.Regulator.Reward).Insigne, ((MockCard)_deserialGame.Regulator.Reward).Insigne);
//            Assert.AreEqual(_toSerialGame.Regulator.Reward.ParentTypeName, _deserialGame.Regulator.Reward.ParentTypeName);
//            Assert.IsNotNull(_toSerialGame.Regulator.Reward.CardSet);
//            Assert.IsNotNull(_deserialGame.Regulator.Reward.CardSet);
//            Assert.AreEqual(_toSerialGame.Regulator.Reward.IsTradeable, _deserialGame.Regulator.Reward.IsTradeable);
//        }
//    }
//}
