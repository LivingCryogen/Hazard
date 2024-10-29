using Model.DataAccess;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using System.Collections.ObjectModel;

namespace Shared.Geography; 

public static class WorldGeography
{
    public static int NumTerritories { get; private set; }
    public static int NumContinents { get; private set; }
    public static ReadOnlyDictionary<ContID, HashSet<TerrID>> ContinentMembers { get; private set; }
    public static ReadOnlyDictionary<TerrID, ContID> TerrIDToContID { get; private set; }

    public static void Initialize(GeographyInitializer initializer)
    {
        NumTerritories = initializer.TerritoryNames.Length - 1; // -1 to accomodate ID.Null
        NumContinents = initializer.ContinentNames.Length - 1; // -1 to accomodate ID.Null

        Dictionary<ContID, HashSet<TerrID>> continentMembers = [];
        Dictionary<TerrID, ContID> terrIDToContID = [];
        foreach(var contTerrPair in initializer.ContinentMembers) {
            if (contTerrPair.Key is not ContID continent)
                continue;

            foreach (Enum territoryEnum in contTerrPair.Value) {
                if (territoryEnum is not TerrID territory)
                    continue;
                continentMembers[continent].Add(territory);
                terrIDToContID[territory] = continent;
            }
        }
        ContinentMembers = new(continentMembers);
        TerrIDToContID = new(terrIDToContID);
    }
    /// <summary>
    /// Determines which continent contains a specified territory.
    /// </summary>
    /// <param name="terrID">The specified territory.</param>
    /// <returns>The containing continent.</returns>
    //public static ContID TerrIDToContID(TerrID terrID)
    //{
    //    return (int)terrID switch {
    //        -1 => ContID.Null,
    //        int n when n >= 0 && n <= 8 => ContID.NorthAmerica,
    //        int n when n >= 9 && n <= 12 => ContID.SouthAmerica,
    //        int n when n >= 13 && n <= 19 => ContID.Europe,
    //        int n when n >= 20 && n <= 25 => ContID.Africa,
    //        int n when n >= 26 && n <= 37 => ContID.Asia,
    //        int n when n >= 38 && n <= 41 => ContID.Oceania,
    //        _ => throw new ArgumentOutOfRangeException(nameof(terrID))
    //    };
    //}

    public static HashSet<TerrID> GetContinentMembers(ContID continent)
    {
        return (int)continent switch {
            -1 => [TerrID.Null],

        };
        /// <summary>
        /// Determines whether a given set of territories fully encompasses a specified continent.
        /// </summary>
        /// <param name="territoryList">The territories to test.</param>
        /// <param name="continent">The continet which may fall within <paramref name="territoryList"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="territoryList"/> includes all of the territories within <paramref name="continent"/>; otherwise, <see langword="false"/>.</returns>
        static bool IncludesContinent(HashSet<TerrID> territoryList, ContID continent)
        {
            foreach (TerrID territory in territoryList)
                if (TerrIDToContID(territory) != continent)
            var continentList = ContinentMembers[continent];
            return continentList.All(territoryList.Contains) && continentList.Count > 0;
        }
    }

    /// <summary>
    /// Gets a mapping of territories to the their neighbors.
    /// </summary>
    /// <remarks>
    /// A naive implementation of a Graph, this is performant enough for small graphs and search depths (~2). But it should be replaced <br/>
    /// if an extension or modification will require signifacantly large graphs or deeper searches.
    /// </remarks>
    // public ReadOnlyDictionary<TerrID, HashSet<TerrID>> NeighborWeb { get; } = new(new Dictionary<TerrID, HashSet<TerrID>>());
}