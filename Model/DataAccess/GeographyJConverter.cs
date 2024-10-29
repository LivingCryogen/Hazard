using Shared.Geography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Model.DataAccess;

public class GeographyJConverter : JsonConverter<GeographyInitializer>
{
    private string GetErrorMessage(Utf8JsonReader reader, string expected)
    {
        return string.Concat(expected, $" was expected by {nameof(reader)} but was not found.");
    }

    public override GeographyInitializer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        GeographyInitializer initializer = new();

        reader.Read();
        if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("exlude"))
            reader.Skip();

        if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals("EnumNames"))
            throw new InvalidDataException(GetErrorMessage(reader, "EnumNames"));
        reader.Read(); // move to value
        string continentEnumName = reader.GetString() ?? throw new InvalidDataException(GetErrorMessage(reader, "Continent Enum name"));
        string territoryEnumName = reader.GetString() ?? throw new InvalidDataException(GetErrorMessage(reader, "Territory Enum name"));
        initializer.SetEnumTypes((continentEnumName, territoryEnumName));
        reader.Read(); // move past end of the Array

        int numContinents = initializer.ContinentNames.Length - 1; // -1 to accomodate .Null
        for (int i = 0; i < numContinents; i++) {
            reader.Read(); // move to Propety Name
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new InvalidDataException(GetErrorMessage(reader, "A PropertyName"));
            reader.Read(); // move to value
            string continentName = reader.GetString() ?? throw new InvalidDataException(GetErrorMessage(reader, $"A valid Continent name"));
            if (!initializer.ContinentNames.Contains(continentName))
                throw new InvalidDataException(GetErrorMessage(reader, $"A valid Continent name in {continentEnumName}"));

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
                Type continentType = initializer.ContinentEnumType ?? throw new InvalidDataException($"{initializer.ContinentEnumType} was invalid.");
                string territoryName = reader.GetString() ?? throw new InvalidDataException(GetErrorMessage(reader, $"A valid Territory name"));
                if (!initializer.TerritoryNames.Contains(territoryName))
                    throw new InvalidDataException($"Continent member Territory name for {initializer} was invalid.");
                initializer.AddContinentMember(continentName, territoryName);
            }
        }

        int numTerritories = initializer.TerritoryNames.Length - 1; // -1 to accomodate .Null
        for (int i = 0; i < numTerritories; i++) {
            reader.Read(); // move to Propety Name
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new InvalidDataException(GetErrorMessage(reader, "A PropertyName"));
            string territoryName = reader.GetString() ?? throw new InvalidDataException(GetErrorMessage(reader, $"A valid Territory name"));
            if (!initializer.TerritoryNames.Contains(territoryName))
                throw new InvalidDataException($"Continent member Territory name for {initializer} was invalid.");

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
                string neighborName = reader.GetString() ?? throw new InvalidDataException(GetErrorMessage(reader, $"A valid Territory name"));
                initializer.AddTerritoryNeighbor(territoryName, neighborName);
            }
        }

        return initializer;
    }

    public override void Write(Utf8JsonWriter writer, GeographyInitializer value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
