using Shared.Geography.Enums;
using Shared.Geography;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model.Tests.Fixtures.Mocks;

namespace Model.Tests.Fixtures;

public static class MockGeography 
{
    private static ReadOnlyDictionary<ContID, HashSet<TerrID>>? _continentMembers;
    private static ReadOnlyDictionary<TerrID, ContID>? _terrIDToContID;
    private static ReadOnlyDictionary<TerrID, HashSet<TerrID>>? _neighborWeb;
    public static int NumTerritories { get; private set; } = 0;
    public static int NumContinents { get; private set; } = 0;

    static MockGeography()
    {
        Initialize(new MockGeographyInitializer());
    }

    public static void Initialize(MockGeographyInitializer initializer)
    {
        NumTerritories = initializer.TerritoryNames.Length - 1; // -1 to accomodate ID.Null
        NumContinents = initializer.ContinentNames.Length - 1; // -1 to accomodate ID.Null

        Dictionary<ContID, HashSet<TerrID>> continentMembers = [];
        Dictionary<TerrID, ContID> terrIDToContID = [];
        Dictionary<TerrID, HashSet<TerrID>> neighborWeb = [];
        foreach (var contTerrPair in initializer.ContinentMembers) {
            if (contTerrPair.Key is not ContID continent)
                continue;

            foreach (Enum territoryEnum in contTerrPair.Value) {
                if (territoryEnum is not TerrID territory)
                    continue;
                continentMembers[continent].Add(territory);
                terrIDToContID[territory] = continent;

                if (!initializer.TerritoryNeighbors.TryGetValue(territory, out HashSet<Enum>? neighbors) || neighbors == null)
                    continue;

                foreach (Enum terrEnum in neighbors) {
                    if (terrEnum is not TerrID neighborTerritory)
                        continue;
                    neighborWeb[territory].Add(neighborTerritory);
                }
            }
        }
        _continentMembers = new(continentMembers);
        _terrIDToContID = new(terrIDToContID);
        _neighborWeb = new(neighborWeb);
    }

    public static ContID TerritoryToContinent(TerrID terrID)
    {
        if (_terrIDToContID == null)
            return ContID.Null;
        if (!_terrIDToContID.TryGetValue(terrID, out var continent))
            return ContID.Null;
        return continent;
    }

    public static HashSet<TerrID> GetContinentMembers(ContID continent)
    {
        if (_continentMembers == null)
            return [];
        if (!_continentMembers.TryGetValue(continent, out var members) || members == null)
            return [];
        return members;
    }
    public static bool IncludesContinent(HashSet<TerrID> territoryList, ContID continent)
    {
        if (_continentMembers == null)
            return false;
        if (!_continentMembers.TryGetValue(continent, out var continentMembers) || continentMembers is not HashSet<TerrID> contMembers)
            return false;
        if (contMembers.Count <= 0 || territoryList.Count <= 0)
            return false;
        if (!contMembers.IsSubsetOf(territoryList))
            return false;
        return true;
    }

    public static HashSet<TerrID> GetNeighbors(TerrID territory)
    {
        if (_neighborWeb == null)
            return [];
        if (!_neighborWeb.TryGetValue(territory, out HashSet<TerrID>? neighbors) || neighbors == null)
            return [];
        return neighbors;
    }
}
