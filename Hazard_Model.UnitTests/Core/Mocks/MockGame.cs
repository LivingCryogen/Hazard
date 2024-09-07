using Hazard_Model.Core;
using Hazard_Model.Entities;
using Hazard_Model.Tests.Entities.Mocks;
using Hazard_Model.Tests.Fixtures.Stubs;
using Hazard_Share.Enums;
using Hazard_Share.Interfaces.Model;
using Microsoft.Extensions.Logging;

namespace Hazard_Model.Tests.Core.Mocks;

public class MockGame : IGame
{
    private readonly LoggerStub<MockGame> _logger = new();

    public MockGame()
    {
        ID = new Guid();
        Players = [new MockPlayer(_logger) { Number = 0 }, new MockPlayer(_logger) { Number = 1 }];
        State = new StateMachine(Players.Count);
        Board = new MockBoard();
        Cards = new MockCardBase(_logger);
        Regulator = new MockRegulator(_logger);
        Values = new MockRuleValues();
    }

    public ILogger<MockGame> Logger { get => _logger; }
    public IBoard? Board { get; set; }
    public IRegulator? Regulator { get; set; }
    public IRuleValues? Values { get; set; }
    public Guid? ID { get; set; }
    public bool DefaultCardMode { get; set; } = true;
    public List<IPlayer> Players { get; set; }
    public StateMachine? State { get; set; }
    public MockCardBase? Cards { get; set; }
    CardBase? IGame.Cards { get => (CardBase?)Cards; set { Cards = (MockCardBase?)value; } }

#pragma warning disable CS0414 // For unit-testing, these are unused. If integration tests are built, they should be, at which time these warnings should be re-enabled.
    public event EventHandler<int>? PlayerLost = null;
    public event EventHandler<int>? PlayerWon = null;
#pragma warning restore CS0414
    public void Wipe()
    {
        ID = null;
        Players.Clear();
        State = new StateMachine(2); // 2 - 6 is allowed
        ((MockGeography?)(Board?.Geography))?.Wipe();
        Board?.Armies?.Clear();
        Board?.ContinentOwner?.Clear();
        Cards?.Reset();
        ((MockRegulator?)Regulator)?.Wipe();
    }
    public void AutoBoard()
    {
        if (Board == null) return;
        if (Players.Count != 2) return;

        // Distribute all initial territories between the two players and a "dummy AI" player randomly
        int numTerritories = Board.Geography.NumTerritories / 3;
        int[] playerPool = [numTerritories, numTerritories, numTerritories];
        Random rand = new();
        byte poolsEmpty = 0b000; // bitwise flags
        byte[] masks = [0b001, 0b010, 0b100]; // flag bitwise manipulators

        for (int i = 0; i < Board!.Geography.NumTerritories; i++) {
            // select the random player, making sure not to select a player without any selections left
            int player;
            switch (poolsEmpty) {
                case 0:
                    player = rand.Next(0, 3);
                    break;
                case 0b1:
                    player = rand.Next(1, 3);
                    break;
                case 0b10:
                    player = rand.Next(0, 2);
                    if (player == 1)
                        player++;
                    break;
                case 0b100:
                    player = rand.Next(0, 2);
                    break;
                case 0b1 | 0b10:
                    player = 2;
                    break;
                case 0b1 | 0b100:
                    player = 1;
                    break;
                case 0b10 | 0b100:
                    player = 0;
                    break;
                default:
                    player = -1;
                    break;
            }

            if (player < 2 && player > -1) {
                Board.Claims(player, (TerrID)i, 1);
                Players[player].AddTerritory((TerrID)i);
                playerPool[player]--;
                if (playerPool[player] <= 0)
                    poolsEmpty |= masks[player];  // If a player's pool is emptied, trip the PoolsEmpty flag at the appropriate bit 
            }
            else if (player == 2) {
                Board.Claims(-1, (TerrID)i, 1);
                playerPool[player]--;
                if (playerPool[player] <= 0)
                    poolsEmpty |= masks[player];  // If a player's pool is emptied, trip the PoolsEmpty flag at the appropriate bit 
            }
        }
    }

    public void Initialize(string[] names)
    {
        throw new NotImplementedException();
    }

    public void Initialize(FileStream openStream)
    {
        throw new NotImplementedException();
    }

    public Task Save(bool isNewFile, string fileName, string precedingData)
    {
        throw new NotImplementedException();
    }
}
