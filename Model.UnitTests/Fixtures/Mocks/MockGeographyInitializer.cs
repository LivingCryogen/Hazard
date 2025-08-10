using System.Reflection;

namespace Model.Tests.Fixtures.Mocks;

public class MockGeographyInitializer
{
    private readonly string _assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;
    public MockGeographyInitializer()
    {
        SetEnumTypes((nameof(ContID), nameof(TerrID)));
    }
    public Type? ContinentEnumType { get; set; }
    public Type? TerritoryEnumType { get; set; }
    public string[] ContinentNames { get; private set; } = [];
    public string[] TerritoryNames { get; private set; } = [];
    public Dictionary<Enum, HashSet<Enum>> ContinentMembers { get; } = new() {
        { ContID.UnitedStates, [
            TerrID.Alabama,
            TerrID.Alaska,
            TerrID.Arizona,
            TerrID.Arkansas,
            TerrID.California,
            TerrID.Colorado,
            TerrID.Connecticut,
            TerrID.Delaware,
            TerrID.Florida,
            TerrID.Georgia,
            TerrID.Hawaii,
            TerrID.Idaho,
            TerrID.Illinois,
            TerrID.Indiana,
            TerrID.Iowa,
            TerrID.Kansas,
            TerrID.Kentucky,
            TerrID.Louisiana,
            TerrID.Maine,
            TerrID.Maryland,
            TerrID.Massachusetts,
            TerrID.Michigan,
            TerrID.Minnesota,
            TerrID.Mississippi,
            TerrID.Missouri,
            TerrID.Montana,
            TerrID.Nebraska,
            TerrID.Nevada,
            TerrID.NewHampshire,
            TerrID.NewJersey,
            TerrID.NewMexico,
            TerrID.NewYork,
            TerrID.NorthCarolina,
            TerrID.NorthDakota,
            TerrID.Ohio,
            TerrID.Oklahoma,
            TerrID.Oregon,
            TerrID.Pennsylvania,
            TerrID.RhodeIsland,
            TerrID.SouthCarolina,
            TerrID.SouthDakota,
            TerrID.Tennessee,
            TerrID.Texas,
            TerrID.Utah,
            TerrID.Vermont,
            TerrID.Virginia,
            TerrID.Washington,
            TerrID.WestVirginia,
            TerrID.Wisconsin,
            TerrID.Wyoming, ]}
        };
    public Dictionary<Enum, HashSet<Enum>> TerritoryNeighbors { get; } = new() {
        { TerrID.Washington, [TerrID.Oregon, TerrID.Idaho] },
        { TerrID.Oregon, [TerrID.Washington, TerrID.Idaho, TerrID.Nevada, TerrID.California] },
        { TerrID.California, [TerrID.Oregon, TerrID.Nevada, TerrID.Arizona] },
        { TerrID.Idaho, [TerrID.Washington, TerrID.Oregon, TerrID.Nevada, TerrID.Utah, TerrID.Wyoming, TerrID.Montana] },
        { TerrID.Nevada, [TerrID.Oregon, TerrID.California, TerrID.Arizona, TerrID.Utah, TerrID.Idaho] },
        { TerrID.Montana, [TerrID.Idaho, TerrID.Wyoming, TerrID.NorthDakota, TerrID.SouthDakota] },
        { TerrID.Wyoming, [TerrID.Montana, TerrID.Idaho, TerrID.Utah, TerrID.Colorado, TerrID.Nebraska, TerrID.SouthDakota] },
    };

    public void SetEnumTypes((string ContinentEnumName, string TerritoryEnumName) names)
    {
        if (Type.GetType(string.Concat("Hazard.Model.Tests.Fixtures.Mocks.", names.ContinentEnumName, _assemblyName)) is not Type continentEnumType)
            throw new InvalidDataException($"");
        ContinentEnumType = continentEnumType;
        if (Type.GetType(string.Concat("Hazard.Model.Tests.Fixtures.Mocks.", names.TerritoryEnumName, _assemblyName)) is not Type territoryEnumType)
            throw new InvalidDataException($"");
        TerritoryEnumType = territoryEnumType;

        ContinentNames = Enum.GetNames(ContinentEnumType);
        TerritoryNames = Enum.GetNames(TerritoryEnumType);
    }

    public bool AddContinentMember(string continentName, string territoryName)
    {
        if (ContinentEnumType == null || TerritoryEnumType == null)
            return false;
        if (Enum.ToObject(ContinentEnumType, continentName) is not Enum continentEnum)
            return false;
        if (Enum.ToObject(TerritoryEnumType, territoryName) is not Enum territoryEnum)
            return false;
        try
        {
            ContinentMembers[continentEnum].Add(territoryEnum);
        }
        catch
        {
            return false;
        }
        return true;
    }

    public bool AddTerritoryNeighbor(string territoryName, string neighborName)
    {
        if (TerritoryEnumType == null)
            return false;
        if (Enum.ToObject(TerritoryEnumType, territoryName) is not Enum territoryEnum)
            return false;
        if (Enum.ToObject(TerritoryEnumType, neighborName) is not Enum neighborEnum)
            return false;
        TerritoryNeighbors[territoryEnum].Add(neighborEnum);
        return true;
    }
}
