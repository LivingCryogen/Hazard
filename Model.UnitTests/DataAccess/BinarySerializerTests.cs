﻿using Model.Tests.Core.Mocks;
using Model.Tests.Entities.Mocks;
using Model.Tests.Fixtures;
using Model.Tests.Fixtures.Mocks;
using Model.Tests.Fixtures.Stubs;
using Shared.Enums;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;

namespace Model.Tests.DataAccess;

[TestClass]
public class BinarySerializerTests
{
    private readonly MockGame _toSerialGame = new();
    private readonly MockGame _deserialGame = new();
    private string _testFileName = string.Empty;

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
        if (BinarySerializer.Load([_deserialGame.Board], _testFileName))
        {
            Assert.IsNotNull(_toSerialGame.Board);
            Assert.IsNotNull(_deserialGame.Board);
            foreach (var contKey in _toSerialGame.Board.ContinentOwner.Keys)
                Assert.AreEqual(_toSerialGame.Board.ContinentOwner[contKey], _deserialGame.Board.ContinentOwner[contKey]);
            foreach (var terrKey in _toSerialGame.Board.TerritoryOwner.Keys)
            {
                Assert.AreEqual(_toSerialGame.Board.TerritoryOwner[terrKey], _deserialGame.Board.TerritoryOwner[terrKey]);
                Assert.AreEqual(_toSerialGame.Board.Armies[terrKey], _deserialGame.Board.Armies[terrKey]);
            }
        }
        else { Assert.Fail(); }
    }

    [TestMethod]
    public async Task Players_RoundTrip_Match()
    {
        var mockSet = _toSerialGame.Cards.Sets[0];
        _toSerialGame.Players.Clear();
        _toSerialGame.Players.Add(new MockPlayer(0, _toSerialGame.Cards.CardFactory, _toSerialGame.Board, new LoggerStubT<MockPlayer>())
        {
            Name = "TestPlayer1",
            ArmyPool = 50,
            ControlledTerritories = [MockTerrID.Alabama, MockTerrID.Idaho, MockTerrID.SouthCarolina],
            Hand = [new MockCard(mockSet)],
        });
        _toSerialGame.Players.Add(new MockPlayer(1, _toSerialGame.Cards.CardFactory, _toSerialGame.Board, new LoggerStubT<MockPlayer>())
        {
            Name = "TestPlayer2",
            ArmyPool = 40,
            ControlledTerritories = [MockTerrID.SouthDakota, MockTerrID.Oklahoma, MockTerrID.Montana],
            Hand = [new MockCard(mockSet), new MockCard(mockSet)],
        });
        _toSerialGame.Players.Add(new MockPlayer(2, _toSerialGame.Cards.CardFactory, _toSerialGame.Board, new LoggerStubT<MockPlayer>())
        {
            Name = "TestPlayer3",
            ArmyPool = 30,
            ControlledTerritories = [MockTerrID.Mississippi],
            Hand = [],
        });
        _toSerialGame.Players.Add(new MockPlayer(3, _toSerialGame.Cards.CardFactory, _toSerialGame.Board, new LoggerStubT<MockPlayer>())
        {
            Name = "TestPlayer4",
            ArmyPool = 20,
            ControlledTerritories = [],
            Hand = [new MockCard(mockSet), new MockCard(mockSet), new MockCard(mockSet)],
            HasCardSet = true
        });
        _toSerialGame.Players.Add(new MockPlayer(4, _toSerialGame.Cards.CardFactory, _toSerialGame.Board, new LoggerStubT<MockPlayer>())
        {
            Name = "TestPlayer5",
            ArmyPool = 10,
            ControlledTerritories = [MockTerrID.California, MockTerrID.Arkansas, MockTerrID.Colorado],
            Hand = [new MockCard(mockSet), new MockCard(mockSet)],
        });
        _toSerialGame.Players.Add(new MockPlayer(5, _toSerialGame.Cards.CardFactory, _toSerialGame.Board, new LoggerStubT<MockPlayer>())
        {
            Name = "TestPlayer6",
            ArmyPool = 0,
            ControlledTerritories = [MockTerrID.Connecticut, MockTerrID.Delaware, MockTerrID.Florida, MockTerrID.Hawaii, MockTerrID.Indiana],
            Hand = [new MockCard(mockSet), new MockCard(mockSet), new MockCard(mockSet), new MockCard(mockSet)],
            HasCardSet = true
        });
        _deserialGame.Players.Clear();
        ((MockCardBase)_deserialGame.Cards).Wipe();

        for (int i = 0; i < _toSerialGame.Players.Count; i++)
        {
            await BinarySerializer.Save([_toSerialGame.Players[i]], _testFileName, true);
            _deserialGame.Players.Add(new MockPlayer(i, _deserialGame.Cards.CardFactory, _deserialGame.Board, new LoggerStubT<MockPlayer>()));
            if (BinarySerializer.Load([_deserialGame.Players[i]], _testFileName))
            {
                Assert.AreEqual(_toSerialGame.Players[i].Number, _deserialGame.Players[i].Number);
                Assert.AreEqual(_toSerialGame.Players[i].Name, _deserialGame.Players[i].Name);
                Assert.AreEqual(_toSerialGame.Players[i].HasCardSet, _deserialGame.Players[i].HasCardSet);
                Assert.AreEqual(_toSerialGame.Players[i].ArmyPool, _deserialGame.Players[i].ArmyPool);
                Assert.AreEqual(_toSerialGame.Players[i].ContinentBonus, _deserialGame.Players[i].ContinentBonus);
                Assert.AreEqual(_toSerialGame.Players[i].HasCardSet, _deserialGame.Players[i].HasCardSet);
                Assert.AreEqual(_toSerialGame.Players[i].ControlledTerritories.Count, _deserialGame.Players[i].ControlledTerritories.Count);
                Assert.IsTrue(_toSerialGame.Players[i].ControlledTerritories.SetEquals(_deserialGame.Players[i].ControlledTerritories));
                Assert.AreEqual(_toSerialGame.Players[i].Hand.Count, _deserialGame.Players[i].Hand.Count);
                for (int j = 0; j < _toSerialGame.Players[i].Hand.Count; j++)
                {
                    Assert.AreEqual(_toSerialGame.Players[i].Hand[j].ParentTypeName, _deserialGame.Players[i].Hand[j].ParentTypeName);
                    Assert.AreEqual(_toSerialGame.Players[i].Hand[j].Target.Length, _deserialGame.Players[i].Hand[j].Target.Length);
                    for (int k = 0; k < _toSerialGame.Players[i].Hand[j].Target.Length; k++)
                    {
                        Assert.AreEqual(_toSerialGame.Players[i].Hand[j].Target[k], _deserialGame.Players[i].Hand[j].Target[k]);
                        Assert.AreEqual(((ITroopCard<MockTerrID>)(_toSerialGame.Players[i].Hand[j])).Insigne, ((ITroopCard<MockTerrID>)(_deserialGame.Players[i].Hand[j])).Insigne);
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

        if (BinarySerializer.Load([_deserialGame.Cards], _testFileName))
        {
            Assert.IsNotNull(_toSerialGame.Cards.Sets);
            Assert.IsNotNull(_deserialGame.Cards.Sets);
            Assert.AreEqual(_toSerialGame.Cards.Sets.Count, _deserialGame.Cards.Sets.Count);
            for (int i = 0; i < _toSerialGame.Cards.Sets.Count; i++)
            {
                Assert.AreEqual(_toSerialGame.Cards.Sets[i].TypeName, _deserialGame!.Cards.Sets[i].TypeName);
                Assert.AreEqual(_toSerialGame.Cards.Sets[i].MemberTypeName, _deserialGame!.Cards.Sets[i].MemberTypeName);
                Assert.IsNotNull(_toSerialGame.Cards.Sets[i].Cards);
                Assert.IsNotNull(_deserialGame.Cards.Sets[i].Cards);
                Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards.Count, _deserialGame.Cards.Sets[i].Cards.Count);
                for (int j = 0; j < _toSerialGame.Cards.Sets[i].Cards.Count; j++)
                {
                    Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards[j].ParentTypeName, _deserialGame!.Cards.Sets[i].Cards![j].ParentTypeName);
                    Assert.IsNotNull(_toSerialGame.Cards.Sets[i].Cards[j].SerializablePropertyNames);
                    Assert.IsNotNull(_deserialGame.Cards.Sets[i].Cards[j].SerializablePropertyNames);
                    Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards[j].SerializablePropertyNames.Count, _deserialGame.Cards.Sets[i].Cards[j].SerializablePropertyNames.Count);
                    Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards[j].IsTradeable, _deserialGame.Cards.Sets[i].Cards[j].IsTradeable);
                    Assert.IsNotNull(_toSerialGame.Cards.Sets[i].Cards[j].CardSet);
                    Assert.IsNotNull(_deserialGame.Cards.Sets[i].Cards[j].CardSet);
#pragma warning disable CS8602 // for some reason the compiler doesn't recognize that the previous two null checks should affect this next Assert.
                    Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards[j].CardSet.TypeName, _deserialGame.Cards.Sets[i].Cards[j].CardSet.TypeName);
#pragma warning restore CS8602
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
            for (int j = 0; j < _toSerialGame.Cards.GameDeck.Library.Count; j++)
            {
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].ParentTypeName, _deserialGame!.Cards.GameDeck.Library[j].ParentTypeName);
                Assert.IsNotNull(_toSerialGame.Cards.GameDeck.Library[j].SerializablePropertyNames);
                Assert.IsNotNull(_deserialGame.Cards.GameDeck.Library[j].SerializablePropertyNames);
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].SerializablePropertyNames.Count, _deserialGame.Cards.GameDeck.Library[j].SerializablePropertyNames.Count);
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].IsTradeable, _deserialGame.Cards.GameDeck.Library[j].IsTradeable);
                Assert.IsNotNull(_toSerialGame.Cards.GameDeck.Library[j].CardSet);
                Assert.IsNotNull(_deserialGame.Cards.GameDeck.Library[j].CardSet);
#pragma warning disable CS8602 // for some reason the compiler doesn't recognize that the previous two null checks should affect this next Assert.
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].CardSet.TypeName, _deserialGame.Cards.GameDeck.Library[j].CardSet.TypeName);
#pragma warning restore CS8602
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].Target[0], _deserialGame.Cards.GameDeck.Library[j].Target[0]); // could test the entire array but the default Targets are always length 1
            }
            Assert.IsNotNull(_toSerialGame.Cards.GameDeck.DiscardPile);
            Assert.IsNotNull(_deserialGame.Cards.GameDeck.DiscardPile);
            for (int j = 0; j < _toSerialGame.Cards.GameDeck.DiscardPile.Count; j++)
            {
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].ParentTypeName, _deserialGame!.Cards.GameDeck.DiscardPile[j].ParentTypeName);
                Assert.IsNotNull(_toSerialGame.Cards.GameDeck.DiscardPile[j].SerializablePropertyNames);
                Assert.IsNotNull(_deserialGame.Cards.GameDeck.DiscardPile[j].SerializablePropertyNames);
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].SerializablePropertyNames.Count, _deserialGame.Cards.GameDeck.DiscardPile[j].SerializablePropertyNames.Count);
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].IsTradeable, _deserialGame.Cards.GameDeck.DiscardPile[j].IsTradeable);
                Assert.IsNotNull(_toSerialGame.Cards.GameDeck.DiscardPile[j].CardSet);
                Assert.IsNotNull(_deserialGame.Cards.GameDeck.DiscardPile[j].CardSet);
#pragma warning disable CS8602 // for some reason the compiler doesn't recognize that the previous two null checks should affect this next Assert.
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].CardSet.TypeName, _deserialGame.Cards.GameDeck.DiscardPile[j].CardSet.TypeName);
#pragma warning restore CS8602
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

        await BinarySerializer.Save([_toSerialGame.State], _testFileName, true);

        if (BinarySerializer.Load([_deserialGame.State], _testFileName))
        {
            Assert.AreEqual(_toSerialGame.State.NumTrades, _deserialGame.State.NumTrades);
            Assert.AreEqual(_toSerialGame.State.CurrentPhase, _deserialGame.State.CurrentPhase);
            Assert.AreEqual(_toSerialGame.State.Round, _deserialGame.State.Round);
            Assert.AreEqual(_toSerialGame.State.PlayerTurn, _deserialGame.State.PlayerTurn);
            Assert.AreEqual(_toSerialGame.State.PhaseStageTwo, _deserialGame.State.PhaseStageTwo);
        }
        else Assert.Fail();
    }

    [TestMethod]
    public async Task Regulator_RoundTrip_Match()
    {
        MockCard rewardCard = new(new MockCardSet())
        {
            Target = [MockTerrID.Idaho],
            Insigne = MockCard.Insignia.FighterJet
        };
        rewardCard.FillTestValues();
        _toSerialGame.Regulator.Reward = rewardCard;
        _toSerialGame.Regulator.CurrentActionsLimit = 7;
        _deserialGame.Regulator.Initialize();

        await BinarySerializer.Save([_toSerialGame.Regulator], _testFileName, true);

        if (BinarySerializer.Load([_deserialGame.Regulator], _testFileName))
        {
            Assert.IsNotNull(_toSerialGame.Regulator.Reward);
            Assert.IsNotNull(_deserialGame.Regulator.Reward);
            Assert.AreEqual(_toSerialGame.Regulator.Reward.Target[0], _deserialGame.Regulator.Reward.Target[0]);
        }
        else Assert.Fail();
    }

    [TestMethod]
    public async Task EntireGame_RoundTrip_Match()
    {
        _toSerialGame.State.NumTrades = 4;
        _toSerialGame.State.CurrentPhase = GamePhase.Attack;
        _toSerialGame.State.Round = 8;
        _toSerialGame.State.PlayerTurn = 1;
        _toSerialGame.State.PhaseStageTwo = true;
        _toSerialGame.State.Winner = 1;

        MockCard rewardCard = new(new MockCardSet())
        {
            Target = [MockTerrID.Idaho],
            Insigne = MockCard.Insignia.FighterJet
        };
        rewardCard.FillTestValues();
        _toSerialGame.Regulator.Reward = rewardCard;
        _toSerialGame.Regulator.CurrentActionsLimit = 7;

        _toSerialGame.Players.Clear();
        var mockSet = _toSerialGame.Cards.Sets[0];
        _toSerialGame.Players.Add(new MockPlayer(0, _toSerialGame.Cards.CardFactory, _toSerialGame.Board, new LoggerStubT<MockPlayer>())
        {
            Name = "TestPlayer1",
            ArmyPool = 50,
            ControlledTerritories = [MockTerrID.Alabama, MockTerrID.Idaho, MockTerrID.SouthCarolina],
            Hand = [new MockCard(mockSet)],
        });
        _toSerialGame.Players.Add(new MockPlayer(1, _toSerialGame.Cards.CardFactory, _toSerialGame.Board, new LoggerStubT<MockPlayer>())
        {
            Name = "TestPlayer2",
            ArmyPool = 40,
            ControlledTerritories = [MockTerrID.SouthDakota, MockTerrID.Oklahoma, MockTerrID.Montana],
            Hand = [new MockCard(mockSet), new MockCard(mockSet)],
        });
        _toSerialGame.Players.Add(new MockPlayer(2, _toSerialGame.Cards.CardFactory, _toSerialGame.Board, new LoggerStubT<MockPlayer>())
        {
            Name = "TestPlayer3",
            ArmyPool = 30,
            ControlledTerritories = [MockTerrID.Mississippi],
            Hand = [],
        });
        _toSerialGame.Players.Add(new MockPlayer(3, _toSerialGame.Cards.CardFactory, _toSerialGame.Board, new LoggerStubT<MockPlayer>())
        {
            Name = "TestPlayer4",
            ArmyPool = 20,
            ControlledTerritories = [],
            Hand = [new MockCard(mockSet), new MockCard(mockSet), new MockCard(mockSet)],
            HasCardSet = true
        });
        _toSerialGame.Players.Add(new MockPlayer(4, _toSerialGame.Cards.CardFactory, _toSerialGame.Board, new LoggerStubT<MockPlayer>())
        {
            Name = "TestPlayer5",
            ArmyPool = 10,
            ControlledTerritories = [MockTerrID.California, MockTerrID.Arkansas, MockTerrID.Colorado],
            Hand = [new MockCard(mockSet), new MockCard(mockSet)],
        });
        _toSerialGame.Players.Add(new MockPlayer(5, _toSerialGame.Cards.CardFactory, _toSerialGame.Board, new LoggerStubT<MockPlayer>())
        {
            Name = "TestPlayer6",
            ArmyPool = 0,
            ControlledTerritories = [MockTerrID.Connecticut, MockTerrID.Delaware, MockTerrID.Florida, MockTerrID.Hawaii, MockTerrID.Indiana],
            Hand = [new MockCard(mockSet), new MockCard(mockSet), new MockCard(mockSet), new MockCard(mockSet)],
            HasCardSet = true
        });
        _deserialGame.Players.Clear();
        ((MockCardBase)_deserialGame.Cards).Wipe();

        await BinarySerializer.Save([_toSerialGame], _testFileName, true);

        if (BinarySerializer.Load([_deserialGame], _testFileName))
        {

            #region BoardAsserts
            Assert.IsNotNull(_toSerialGame.Board);
            Assert.IsNotNull(_deserialGame.Board);
            foreach (var contKey in _toSerialGame.Board.ContinentOwner.Keys)
                Assert.AreEqual(_toSerialGame.Board.ContinentOwner[contKey], _deserialGame.Board.ContinentOwner[contKey]);
            foreach (var terrKey in _toSerialGame.Board.TerritoryOwner.Keys)
            {
                Assert.AreEqual(_toSerialGame.Board.TerritoryOwner[terrKey], _deserialGame.Board.TerritoryOwner[terrKey]);
                Assert.AreEqual(_toSerialGame.Board.Armies[terrKey], _deserialGame.Board.Armies[terrKey]);
            }
            #endregion
            #region PlayersAsserts
            for (int i = 0; i < _toSerialGame.Players.Count; i++)
            {
                Assert.AreEqual(_toSerialGame.Players[i].Number, _deserialGame.Players[i].Number);
                Assert.AreEqual(_toSerialGame.Players[i].Name, _deserialGame.Players[i].Name);
                Assert.AreEqual(_toSerialGame.Players[i].HasCardSet, _deserialGame.Players[i].HasCardSet);
                Assert.AreEqual(_toSerialGame.Players[i].ArmyPool, _deserialGame.Players[i].ArmyPool);
                Assert.AreEqual(_toSerialGame.Players[i].ContinentBonus, _deserialGame.Players[i].ContinentBonus);
                Assert.AreEqual(_toSerialGame.Players[i].ControlledTerritories.Count, _deserialGame.Players[i].ControlledTerritories.Count);
                Assert.IsTrue(_toSerialGame.Players[i].ControlledTerritories.SetEquals(_deserialGame.Players[i].ControlledTerritories));
                Assert.AreEqual(_toSerialGame.Players[i].Hand.Count, _deserialGame.Players[i].Hand.Count);
                for (int j = 0; j < _toSerialGame.Players[i].Hand.Count; j++)
                {
                    Assert.AreEqual(_toSerialGame.Players[i].Hand[j].ParentTypeName, _deserialGame.Players[i].Hand[j].ParentTypeName);
                    Assert.AreEqual(_toSerialGame.Players[i].Hand[j].Target.Length, _deserialGame.Players[i].Hand[j].Target.Length);
                    for (int k = 0; k < _toSerialGame.Players[i].Hand[j].Target.Length; k++)
                    {
                        Assert.AreEqual(_toSerialGame.Players[i].Hand[j].Target[k], _deserialGame.Players[i].Hand[j].Target[k]);
                        Assert.AreEqual(((ITroopCard<MockTerrID>)(_toSerialGame.Players[i].Hand[j])).Insigne, ((ITroopCard<MockTerrID>)_deserialGame.Players[i].Hand[j]).Insigne);
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
            #endregion
            #region CardBaseAsserts
            Assert.IsNotNull(_toSerialGame.Cards.Sets);
            Assert.IsNotNull(_deserialGame.Cards.Sets);
            Assert.AreEqual(_toSerialGame.Cards.Sets.Count, _deserialGame.Cards.Sets.Count);
            for (int i = 0; i < _toSerialGame.Cards.Sets.Count; i++)
            {
                Assert.AreEqual(_toSerialGame.Cards.Sets[i].TypeName, _deserialGame!.Cards.Sets[i].TypeName);
                Assert.AreEqual(_toSerialGame.Cards.Sets[i].MemberTypeName, _deserialGame!.Cards.Sets[i].MemberTypeName);
                Assert.IsNotNull(_toSerialGame.Cards.Sets[i].Cards);
                Assert.IsNotNull(_deserialGame.Cards.Sets[i].Cards);
                Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards.Count, _deserialGame.Cards.Sets[i].Cards.Count - 13); // There are 13 additional cards added from Arranged Players and Regulator.Reward
                for (int j = 0; j < _toSerialGame.Cards.Sets[i].Cards.Count; j++)
                {
                    Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards[j].ParentTypeName, _deserialGame!.Cards.Sets[i].Cards![j].ParentTypeName);
                    Assert.IsNotNull(_toSerialGame.Cards.Sets[i].Cards[j].SerializablePropertyNames);
                    Assert.IsNotNull(_deserialGame.Cards.Sets[i].Cards[j].SerializablePropertyNames);
                    Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards[j].SerializablePropertyNames.Count, _deserialGame.Cards.Sets[i].Cards[j].SerializablePropertyNames.Count);
                    Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards[j].IsTradeable, _deserialGame.Cards.Sets[i].Cards[j].IsTradeable);
                    Assert.IsNotNull(_toSerialGame.Cards.Sets[i].Cards[j].CardSet);
                    Assert.IsNotNull(_deserialGame.Cards.Sets[i].Cards[j].CardSet);
#pragma warning disable CS8602 // for some reason the compiler doesn't recognize that the previous two null checks should affect this next Assert.
                    Assert.AreEqual(_toSerialGame.Cards.Sets[i].Cards[j].CardSet.TypeName, _deserialGame.Cards.Sets[i].Cards[j].CardSet.TypeName);
#pragma warning restore CS8602
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
            for (int j = 0; j < _toSerialGame.Cards.GameDeck.Library.Count; j++)
            {
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].ParentTypeName, _deserialGame!.Cards.GameDeck.Library[j].ParentTypeName);
                Assert.IsNotNull(_toSerialGame.Cards.GameDeck.Library[j].SerializablePropertyNames);
                Assert.IsNotNull(_deserialGame.Cards.GameDeck.Library[j].SerializablePropertyNames);
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].SerializablePropertyNames.Count, _deserialGame.Cards.GameDeck.Library[j].SerializablePropertyNames.Count);
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].IsTradeable, _deserialGame.Cards.GameDeck.Library[j].IsTradeable);
                Assert.IsNotNull(_toSerialGame.Cards.GameDeck.Library[j].CardSet);
                Assert.IsNotNull(_deserialGame.Cards.GameDeck.Library[j].CardSet);
#pragma warning disable CS8602 // for some reason the compiler doesn't recognize that the previous two null checks should affect this next Assert.
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].CardSet.TypeName, _deserialGame.Cards.GameDeck.Library[j].CardSet.TypeName);
#pragma warning restore CS8602
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.Library[j].Target[0], _deserialGame.Cards.GameDeck.Library[j].Target[0]); // could test the entire array but the default Targets are always length 1
            }
            Assert.IsNotNull(_toSerialGame.Cards.GameDeck.DiscardPile);
            Assert.IsNotNull(_deserialGame.Cards.GameDeck.DiscardPile);
            for (int j = 0; j < _toSerialGame.Cards.GameDeck.DiscardPile.Count; j++)
            {
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].ParentTypeName, _deserialGame!.Cards.GameDeck.DiscardPile[j].ParentTypeName);
                Assert.IsNotNull(_toSerialGame.Cards.GameDeck.DiscardPile[j].SerializablePropertyNames);
                Assert.IsNotNull(_deserialGame.Cards.GameDeck.DiscardPile[j].SerializablePropertyNames);
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].SerializablePropertyNames.Count, _deserialGame.Cards.GameDeck.DiscardPile[j].SerializablePropertyNames.Count);
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].IsTradeable, _deserialGame.Cards.GameDeck.DiscardPile[j].IsTradeable);
                Assert.IsNotNull(_toSerialGame.Cards.GameDeck.DiscardPile[j].CardSet);
                Assert.IsNotNull(_deserialGame.Cards.GameDeck.DiscardPile[j].CardSet);
#pragma warning disable CS8602 // for some reason the compiler doesn't recognize that the previous two null checks should affect this next Assert.
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].CardSet.TypeName, _deserialGame.Cards.GameDeck.DiscardPile[j].CardSet.TypeName);
#pragma warning restore CS8602
                Assert.AreEqual(_toSerialGame.Cards.GameDeck.DiscardPile[j].Target[0], _deserialGame.Cards.GameDeck.DiscardPile[j].Target[0]); // could test the entire array but the default Targets are always length 1
            }
            #endregion
            #region StateAsserts
            Assert.AreEqual(_toSerialGame.State.NumTrades, _deserialGame.State.NumTrades);
            Assert.AreEqual(_toSerialGame.State.CurrentPhase, _deserialGame.State.CurrentPhase);
            Assert.AreEqual(_toSerialGame.State.Round, _deserialGame.State.Round);
            Assert.AreEqual(_toSerialGame.State.PlayerTurn, _deserialGame.State.PlayerTurn);
            Assert.AreEqual(_toSerialGame.State.PhaseStageTwo, _deserialGame.State.PhaseStageTwo);
            #endregion
            #region RegulatorAsserts
            Assert.IsNotNull(_toSerialGame.Regulator.Reward);
            Assert.IsNotNull(_deserialGame.Regulator.Reward);
            Assert.AreEqual(_toSerialGame.Regulator.Reward.Target[0], _deserialGame.Regulator.Reward.Target[0]);
            #endregion
        }
        else Assert.Fail();
    }
}
