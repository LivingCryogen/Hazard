using Model.Tests.Fixtures.Mocks;
using Share.Enums;
using Share.Interfaces.Model;
using System.Collections.ObjectModel;

namespace Model.Tests.Entities.Mocks;

public class MockGeography : IGeography
{
    public int NumTerritories { get; set; } = 1;

    public int NumContinents { get; set; } = 1;

    public ReadOnlyDictionary<MockTerrID, List<MockTerrID>>? NeighborWeb { get; set; } = new(new Dictionary<MockTerrID, List<MockTerrID>>());
    ReadOnlyDictionary<TerrID, List<TerrID>>? IGeography.NeighborWeb {
        get {
            if (NeighborWeb == null) return null;
            Dictionary<TerrID, List<TerrID>> castWeb = [];
            var castKeys = NeighborWeb.Keys.Select(id => (TerrID)(int)id).ToArray();
            var castValues = NeighborWeb.Values.Select(list => list.Cast<TerrID>().ToList()).ToArray();
            for (int i = 0; i < castKeys.Length; i++) {
                castWeb.Add(castKeys[i], castValues[i]);
            }
            return new ReadOnlyDictionary<TerrID, List<TerrID>>(castWeb);
        }
    }

    public void Wipe()
    {
        NumTerritories = 0;
        NumContinents = 0;
        NeighborWeb = null;
    }
}
