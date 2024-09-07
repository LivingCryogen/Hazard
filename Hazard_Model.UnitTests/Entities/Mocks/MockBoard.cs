using Hazard_Share.Enums;
using Hazard_Share.Interfaces;
using Hazard_Share.Interfaces.Model;

namespace Hazard_Model.Tests.Entities.Mocks;

internal class MockBoard : IBoard
{
    public List<object> this[int playerNumber, string enumName] => throw new NotImplementedException();

    public MockBoard()
    {
        Armies = [];
        Armies.Add(TerrID.Alaska, 10);
        TerritoryOwner = [];
        TerritoryOwner.Add(TerrID.Alaska, 1);
        ContinentOwner = [];
        ContinentOwner.Add(ContID.NorthAmerica, -1);
    }

    public IGeography Geography => new MockGeography();

    public Dictionary<TerrID, int> Armies { get; init; }
    public Dictionary<TerrID, int> TerritoryOwner { get; init; }
    public Dictionary<ContID, int> ContinentOwner { get; init; }

#pragma warning disable CS0414 // For unit-testing, these are unused. If integration tests are built, they should be, at which time these warnings should be re-enabled.
    public event EventHandler<ITerritoryChangedEventArgs>? TerritoryChanged = null;
    public event EventHandler<IContinentOwnerChangedEventArgs>? ContinentOwnerChanged = null;
#pragma warning restore CS0414

    public void CheckContinentFlip(TerrID changed, int previousOwner)
    {
        throw new NotImplementedException();
    }

    public void Claims(int newPlayer, TerrID territory)
    {
        throw new NotImplementedException();
    }

    public void Claims(int newPlayer, TerrID territory, int armies)
    {
        throw new NotImplementedException();
    }

    public void Conquer(TerrID source, TerrID target, int newOwner)
    {
        throw new NotImplementedException();
    }

    public void Reinforce(TerrID territory)
    {
        throw new NotImplementedException();
    }

    public void Reinforce(TerrID territory, int armies)
    {
        throw new NotImplementedException();
    }
}
