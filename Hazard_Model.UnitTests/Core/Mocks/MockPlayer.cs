using Hazard_Share.Enums;
using Hazard_Share.Interfaces.Model;
using Microsoft.Extensions.Logging;

namespace Hazard_Model.Tests.Core.Mocks;

internal class MockPlayer(ILogger logger) : IPlayer
{
    private readonly ILogger _logger = logger;
    public int ArmyBonus { get; }

    public int ArmyPool { get => 5; set => throw new NotImplementedException(); }
    public int ContinentBonus { get => 6; set => throw new NotImplementedException(); }
    public List<TerrID> ControlledTerritories { get => [TerrID.Null, TerrID.Alaska]; set => throw new NotImplementedException(); }
    public List<ICard> Hand { get => []; set => throw new NotImplementedException(); }
    public string Name { get => nameof(MockPlayer); init => throw new NotImplementedException(); }
    public int Number { get; init; }
    public bool HasCardSet { get => false; set => throw new NotImplementedException(); }

#pragma warning disable CS0414 // For unit-testing, these are unused. If integration tests are built, they should be, at which time these warnings should be re-enabled.
    public event EventHandler<IPlayerChangedEventArgs>? PlayerChanged = null;
    public event EventHandler? PlayerLost = null;
    public event EventHandler? PlayerWon = null;
#pragma warning restore CS0414
    public List<(object? Datum, Type? DataType)> GetSaveData()
    {
        List<(object? Datum, Type? DataType)> data = [
            (Name, typeof(string)),
            (ArmyPool, typeof(int)),
            (ContinentBonus, typeof(int)),
            (ControlledTerritories.Count, typeof(int)) ];
        foreach (TerrID territory in ControlledTerritories)
            data.Add((territory, typeof(TerrID)));
        data.Add((Hand.Count, typeof(int)));
        foreach (ICard card in Hand)
            data.Add((card.GetSaveData(_logger), typeof(ITroopCard)));
        return data;
    }
    public bool AddCard(ICard card)
    {
        throw new NotImplementedException();
    }
    public bool AddTerritory(TerrID territory)
    {
        throw new NotImplementedException();
    }
    public TerrID[] GetControlledTargets(TerrID[] targets)
    {
        throw new NotImplementedException();
    }
    public void GetsTradeBonus(int tradeInBonus)
    {
        throw new NotImplementedException();
    }
    public bool RemoveCard(int handIndex)
    {
        throw new NotImplementedException();
    }
    public bool RemoveTerritory(TerrID territory)
    {
        throw new NotImplementedException();
    }
    public void FindCardSet()
    {
        List<ICardSet> setsInHand = [];
        List<ICard> tradeableCards = [];
        foreach (var card in Hand) {
            if (card.CardSet != null && !setsInHand.Contains(card.CardSet))
                setsInHand.Add(card.CardSet);

            if (card.IsTradeable == true)
                tradeableCards.Add(card);
        }

        if (setsInHand.Count == 0 || tradeableCards.Count == 0)
            HasCardSet = false;

        ICard[] tradeable = [.. tradeableCards];

        foreach (ICardSet cardSet in setsInHand) {
            if ((cardSet?.FindTradeSets(tradeable) ?? []).Length != 0)
                HasCardSet = true;
        }
    }
}
