using Shared.Geography.Enums;
using System.Collections.ObjectModel;

namespace Shared.Geography; 
/// <summary>
/// Encapsulates maps and methods for board Geography.
/// </summary>
public static class BoardGeography
{
    private static ReadOnlyDictionary<ContID, HashSet<TerrID>>? _continentMembers;
    private static ReadOnlyDictionary<TerrID, ContID>? _terrIDToContID;
    private static ReadOnlyDictionary<TerrID, HashSet<TerrID>>? _neighborWeb;
    /// <summary>
    /// Gets the number of territories on the board.
    /// </summary>
    public static int NumTerritories { get; private set; }
    /// <summary>
    /// Gets the number of continents on the board.
    /// </summary>
    public static int NumContinents { get; private set; }
    /// <summary>
    /// Initializes this <see cref="BoardGeography"/> with values from a 'BoardGeography.json' via the DAL.
    /// </summary>
    /// <param name="initializer">The initializer provided by the DAL. Must be registered as <see cref="Services.Registry.RegistryRelation.ConvertedDataType"/> for <see cref="BoardGeography"/>.</param>
    public static void Initialize(GeographyInitializer initializer)
    {
        NumTerritories = initializer.TerritoryNames.Length - 1; // -1 to accomodate ID.Null
        NumContinents = initializer.ContinentNames.Length - 1; // -1 to accomodate ID.Null

        Dictionary<ContID, HashSet<TerrID>> continentMembers = [];
        Dictionary<TerrID, ContID> terrIDToContID = [];
        Dictionary<TerrID, HashSet<TerrID>> neighborWeb = [];
        foreach(var contTerrPair in initializer.ContinentMembers) {
            if (contTerrPair.Key is not ContID continent)
                continue;

            foreach (Enum territoryEnum in contTerrPair.Value) {
                if (territoryEnum is not TerrID territory)
                    continue;

                if (!continentMembers.ContainsKey(continent))
                    continentMembers.Add(continent, []);

                continentMembers[continent].Add(territory);
                terrIDToContID[territory] = continent;
                if (!neighborWeb.ContainsKey(territory))
                    neighborWeb.Add(territory, []);

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
    /// <summary>
    /// Gets the continent containing a given territory.
    /// </summary>
    /// <param name="terrID">The territory contained.</param>
    /// <returns>The continent containing <paramref name="terrID"/>.</returns>
    public static ContID TerritoryToContinent(TerrID terrID)
    {
        if (_terrIDToContID == null)
            return ContID.Null;
        if (!_terrIDToContID.TryGetValue(terrID, out var continent))
            return ContID.Null;
        return continent;
    }
    /// <summary>
    /// Gets the full group of territories within a given continent.
    /// </summary>
    /// <param name="continent">The continent whose member territories should be returned.</param>
    /// <returns>The territories that are within <paramref name="continent"/>.</returns>
    public static HashSet<TerrID> GetContinentMembers(ContID continent)
    {
        if (_continentMembers == null)
            return [];
        if (!_continentMembers.TryGetValue(continent, out var members) || members == null)
            return [];
        return members;
    }
    /// <summary>
    /// Determines whether a group of territories includes the entirety of a given continent.
    /// </summary>
    /// <param name="territoryList">The group of territories that may contain <paramref name="continent"/>.</param>
    /// <param name="continent">The continent that may or may not be contained by <paramref name="territoryList"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="continent"/> is entirely covered by the territories iin <paramref name="territoryList"/>; otherwise, <see langword="false"/>.</returns>
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
    /// <summary>
    /// Gets the territory neighbors of a given territory.
    /// </summary>
    /// <param name="territory">The territory whose neighbors should be returned.</param>
    /// <returns>The neighbors of <paramref name="territory"/>.</returns>
    /// <remarks>
    /// A "neighbor" is a territory directly adjacent to another.
    /// </remarks>
    public static HashSet<TerrID> GetNeighbors(TerrID territory)
    {
        if (_neighborWeb == null)
            return [];
        if (!_neighborWeb.TryGetValue(territory, out HashSet<TerrID>? neighbors) || neighbors == null)
            return [];
        return neighbors;
    }
}