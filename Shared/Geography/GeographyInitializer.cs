using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Reflection;
using System.Text;

namespace Shared.Geography;

public class GeographyInitializer
{
    private readonly string _assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;


    public Type? ContinentEnumType { get; set; }
    public Type? TerritoryEnumType { get; set; }
    public string[] ContinentNames { get; private set; } = [];
    public string[] TerritoryNames { get; private set; } = [];
    public Dictionary<Enum, HashSet<Enum>> ContinentMembers { get; } = [];
    public Dictionary<Enum, HashSet<Enum>> TerritoryNeighbors { get; } = [];

    public void SetEnumTypes((string ContinentEnumName, string TerritoryEnumName) names)
    {
        if (Type.GetType(string.Concat("Hazard.Shared.Geography.Enums.", names.ContinentEnumName, _assemblyName)) is not Type continentEnumType)
            throw new InvalidDataException($"");
        ContinentEnumType = continentEnumType;
        if (Type.GetType(string.Concat("Hazard.Shared.Geography.Enums.", names.TerritoryEnumName, _assemblyName)) is not Type territoryEnumType)
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
