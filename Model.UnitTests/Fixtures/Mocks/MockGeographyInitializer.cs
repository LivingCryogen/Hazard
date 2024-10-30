using Shared.Geography.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Model.Tests.Fixtures.Mocks;

public class MockGeographyInitializer
{
    private readonly string _assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;
    public MockGeographyInitializer()
    {
        SetEnumTypes((nameof(MockContID), nameof(MockTerrID)));
    }
    public Type? ContinentEnumType { get; set; }
    public Type? TerritoryEnumType { get; set; }
    public string[] ContinentNames { get; private set; } = [];
    public string[] TerritoryNames { get; private set; } = [];
    public Dictionary<Enum, HashSet<Enum>> ContinentMembers { get; } = new() {
        { MockContID.UnitedStates, [
            MockTerrID.Alabama,
            MockTerrID.Alaska,
            MockTerrID.Arizona,
            MockTerrID.Arkansas,
            MockTerrID.California,
            MockTerrID.Colorado,
            MockTerrID.Connecticut,
            MockTerrID.Delaware,
            MockTerrID.Florida,
            MockTerrID.Georgia,
            MockTerrID.Hawaii,
            MockTerrID.Idaho,
            MockTerrID.Illinois,
            MockTerrID.Indiana,
            MockTerrID.Iowa,
            MockTerrID.Kansas,
            MockTerrID.Kentucky,
            MockTerrID.Louisiana,
            MockTerrID.Maine,
            MockTerrID.Maryland,
            MockTerrID.Massachusetts,
            MockTerrID.Michigan,
            MockTerrID.Minnesota,
            MockTerrID.Mississippi,
            MockTerrID.Missouri,
            MockTerrID.Montana,
            MockTerrID.Nebraska,
            MockTerrID.Nevada,
            MockTerrID.NewHampshire,
            MockTerrID.NewJersey,
            MockTerrID.NewMexico,
            MockTerrID.NewYork,
            MockTerrID.NorthCarolina,
            MockTerrID.NorthDakota,
            MockTerrID.Ohio,
            MockTerrID.Oklahoma,
            MockTerrID.Oregon,
            MockTerrID.Pennsylvania,
            MockTerrID.RhodeIsland,
            MockTerrID.SouthCarolina,
            MockTerrID.SouthDakota,
            MockTerrID.Tennessee,
            MockTerrID.Texas,
            MockTerrID.Utah,
            MockTerrID.Vermont,
            MockTerrID.Virginia,
            MockTerrID.Washington,
            MockTerrID.WestVirginia,
            MockTerrID.Wisconsin,
            MockTerrID.Wyoming, ]}
        };
    public Dictionary<Enum, HashSet<Enum>> TerritoryNeighbors { get; } = new() {
        { MockTerrID.Washington, [MockTerrID.Oregon, MockTerrID.Idaho] },
        { MockTerrID.Oregon, [MockTerrID.Washington, MockTerrID.Idaho, MockTerrID.Nevada, MockTerrID.California] },
        { MockTerrID.California, [MockTerrID.Oregon, MockTerrID.Nevada, MockTerrID.Arizona] },
        { MockTerrID.Idaho, [MockTerrID.Washington, MockTerrID.Oregon, MockTerrID.Nevada, MockTerrID.Utah, MockTerrID.Wyoming, MockTerrID.Montana] },
        { MockTerrID.Nevada, [MockTerrID.Oregon, MockTerrID.California, MockTerrID.Arizona, MockTerrID.Utah, MockTerrID.Idaho] },
        { MockTerrID.Montana, [MockTerrID.Idaho, MockTerrID.Wyoming, MockTerrID.NorthDakota, MockTerrID.SouthDakota] },
        { MockTerrID.Wyoming, [MockTerrID.Montana, MockTerrID.Idaho, MockTerrID.Utah, MockTerrID.Colorado, MockTerrID.Nebraska, MockTerrID.SouthDakota] },
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
        try {
            ContinentMembers[continentEnum].Add(territoryEnum);
        } catch {
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
