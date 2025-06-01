using Shared.Interfaces.Model;

namespace Shared.Geography;
/// <summary>
/// Initializes <see cref="BoardGeography"/> with data provided by the DAL.
/// </summary>
/// <remarks>
/// This is the target <see cref="Services.Registry.RegistryRelation.ConvertedDataType">converted data type</see> for <see cref="BoardGeography"/> within <see cref="Services.Registry.TypeRegister"/>. <br/>
/// See <see cref="IAssetFetcher.FetchGeography"/>. 
/// </remarks>
public class GeographyInitializer
{
    /// <summary>
    /// Gets or sets the Type variable read from the data file corresponding to <see cref="Enums.ContID"/>.
    /// </summary>
    public Type? ContinentEnumType { get; set; }
    /// <summary>
    /// Gets or sets the Type variable read from the data file corresponding to <see cref="Enums.TerrID"/>.
    /// </summary>
    public Type? TerritoryEnumType { get; set; }
    /// <summary>
    /// Gets the names of the continents as given by the values of <see cref="ContinentEnumType"/>.
    /// </summary>
    public string[] ContinentNames { get; private set; } = [];
    /// <summary>
    /// Gets the names of the territories as given by the values of <see cref="TerritoryEnumType"/>.
    /// </summary>
    public string[] TerritoryNames { get; private set; } = [];
    /// <summary>
    /// Maps continents to their members territories.
    /// </summary>
    public Dictionary<Enum, HashSet<Enum>> ContinentMembers { get; } = [];
    /// <summary>
    /// Maps territories to their neighbors.
    /// </summary>
    public Dictionary<Enum, HashSet<Enum>> TerritoryNeighbors { get; } = [];
    /// <summary>
    /// Prepares this <see cref="GeographyInitializer"/> for mapping by getting the Enums corresponding to <paramref name="names"/>.
    /// </summary>
    /// <param name="names">The names of the Enums that should contain the continents and territories within the board geography.</param>
    /// <exception cref="InvalidDataException">Thrown when Enum types could not be generated from <paramref name="names"/>.</exception>
    public void SetEnumTypes((string ContinentEnumName, string TerritoryEnumName) names)
    {
        if (Type.GetType(names.ContinentEnumName) is not Type continentEnumType || !continentEnumType.IsEnum)
            throw new InvalidDataException($"{this} could not locate a Continent Enum.");
        ContinentEnumType = continentEnumType;
        if (Type.GetType(names.TerritoryEnumName) is not Type territoryEnumType || !territoryEnumType.IsEnum)
            throw new InvalidDataException($"{this} could not locate a Territory Enum.");
        TerritoryEnumType = territoryEnumType;

        ContinentNames = Enum.GetNames(ContinentEnumType);
        TerritoryNames = Enum.GetNames(TerritoryEnumType);
    }
    /// <summary>
    /// Maps a territory to a continent as its member within <see cref="ContinentMembers"/>.
    /// </summary>
    /// <param name="continentName">The name of the parent continent.</param>
    /// <param name="territoryName">The name of the member territory.</param>
    /// <returns><see langword="true"/> if the mapping was successful; otherwise, <see langword="false"/>.</returns>
    public bool AddContinentMember(string continentName, string territoryName)
    {
        if (ContinentEnumType == null || TerritoryEnumType == null)
            return false;
        if (Enum.Parse(ContinentEnumType, continentName) is not Enum continentEnum)
            return false;
        if (Enum.Parse(TerritoryEnumType, territoryName) is not Enum territoryEnum)
            return false;
        try
        {
            if (!ContinentMembers.ContainsKey(continentEnum))
                ContinentMembers.Add(continentEnum, []);

            ContinentMembers[continentEnum].Add(territoryEnum);
        }
        catch
        {
            return false;
        }
        return true;
    }
    /// <summary>
    /// Maps a territory as a neighbor to another within <see cref="TerritoryNeighbors"/>.
    /// </summary>
    /// <param name="territoryName">The name of the territory whose neighbor list should now include <paramref name="neighborName"/>.</param>
    /// <param name="neighborName">The name of the territory to be mapped as a neighbor to <paramref name="territoryName"/>.</param>
    /// <returns><see langword="true"/> if the mapping was successful; otherwise, <see langword="false"/>.</returns>
    public bool AddTerritoryNeighbor(string territoryName, string neighborName)
    {
        if (TerritoryEnumType == null)
            return false;
        if (Enum.Parse(TerritoryEnumType, territoryName) is not Enum territoryEnum)
            return false;
        if (Enum.Parse(TerritoryEnumType, neighborName) is not Enum neighborEnum)
            return false;
        try
        {
            if (!TerritoryNeighbors.ContainsKey(territoryEnum))
                TerritoryNeighbors.Add(territoryEnum, []);

            TerritoryNeighbors[territoryEnum].Add(neighborEnum);
        }
        catch
        {
            return false;
        }
        return true;
    }
}
