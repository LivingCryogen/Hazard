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
        if (Type.GetType(names.ContinentEnumName) is not Type continentEnumType)    
            throw new InvalidDataException($"{this} could not locate a Continent Enum.");
        ContinentEnumType = continentEnumType;
        if (Type.GetType(names.TerritoryEnumName) is not Type territoryEnumType)
            throw new InvalidDataException($"{this} could not locate a Territory Enum.");
        TerritoryEnumType = territoryEnumType;

        ContinentNames = Enum.GetNames(ContinentEnumType);
        TerritoryNames = Enum.GetNames(TerritoryEnumType);
    }

    public bool AddContinentMember(string continentName, string territoryName)
    {
        if (ContinentEnumType == null || TerritoryEnumType == null)
            return false;
        if (Enum.Parse(ContinentEnumType, continentName) is not Enum continentEnum)
            return false;
        if (Enum.Parse(TerritoryEnumType, territoryName) is not Enum territoryEnum)
            return false;
        try {
            if (!ContinentMembers.ContainsKey(continentEnum)) 
                ContinentMembers.Add(continentEnum, []);
            
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
        if (Enum.Parse(TerritoryEnumType, territoryName) is not Enum territoryEnum)
            return false;
        if (Enum.Parse(TerritoryEnumType, neighborName) is not Enum neighborEnum)
            return false;
        try {
            if (!TerritoryNeighbors.ContainsKey(territoryEnum))
                TerritoryNeighbors.Add(territoryEnum, []);

            TerritoryNeighbors[territoryEnum].Add(neighborEnum);
        } catch {
            return false;
        }
        return true;
    }
}
