using Shared.Geography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Model.DataAccess;
/// <summary>
/// The '.json' converter for geography data files.
/// </summary>
/// <remarks>
/// By default, a <see cref="GeographyJConverter"/> should be registered as <see cref="Shared.Services.Registry.RegistryRelation.DataConverter"/> of <see cref="BoardGeography"/> <br/>
/// within <see cref="Shared.Services.Registry.TypeRegister"/>.
/// </remarks>
public class GeographyJConverter : JsonConverter<GeographyInitializer>
{
    private static string GetErrorMessage(string expected)
    {
        return string.Concat(expected, $" was expected by GeographyJConverter but was not found.");
    }
    /// <inheritdoc/>
    public override GeographyInitializer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        GeographyInitializer initializer = new();
        while (reader.Read())
            if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("EnumNames"))
                LoadInitializer(reader, initializer);

        return initializer;
    }
    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, GeographyInitializer value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    private static void LoadInitializer(Utf8JsonReader reader, GeographyInitializer initializer)
    {
        string? continentEnumName = null;
        string? territoryEnumName = null;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
            if (reader.TokenType == JsonTokenType.String && continentEnumName == null) {
                continentEnumName = reader.GetString() ?? throw new InvalidDataException(GetErrorMessage("Continent Enum name"));
                continue;
            }
            if (reader.TokenType == JsonTokenType.String && continentEnumName != null && territoryEnumName == null)
                territoryEnumName = reader.GetString() ?? throw new InvalidDataException(GetErrorMessage("Territory Enum name"));

            if (continentEnumName != null && territoryEnumName != null)
                initializer.SetEnumTypes((continentEnumName, territoryEnumName));
        }

        int numContinents = initializer.ContinentNames.Length - 1; // -1 to accomodate .Null
        for (int i = 0; i < numContinents; i++) {
            reader.Read(); // move to Propety Name
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new InvalidDataException(GetErrorMessage("A PropertyName"));
            string continentName = reader.GetString() ?? throw new InvalidDataException(GetErrorMessage($"A valid Continent name"));
            if (!initializer.ContinentNames.Contains(continentName))
                throw new InvalidDataException(GetErrorMessage($"A valid Continent name in {continentEnumName}"));

            reader.Read(); // move to StartArray
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
                string territoryName = reader.GetString() ?? throw new InvalidDataException(GetErrorMessage($"A valid Territory name"));
                if (!initializer.TerritoryNames.Contains(territoryName))
                    throw new InvalidDataException($"Continent member Territory name for {initializer} was invalid.");
                initializer.AddContinentMember(continentName, territoryName);
            }
        }

        int numTerritories = initializer.TerritoryNames.Length - 1; // -1 to accomodate .Null
        for (int i = 0; i < numTerritories; i++) {
            reader.Read(); // move to Propety Name
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new InvalidDataException(GetErrorMessage("A PropertyName"));
            string territoryName = reader.GetString() ?? throw new InvalidDataException(GetErrorMessage($"A valid Territory name"));
            if (!initializer.TerritoryNames.Contains(territoryName))
                throw new InvalidDataException($"Continent member Territory name for {initializer} was invalid.");

            reader.Read(); // move past StartArray
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
                string neighborName = reader.GetString() ?? throw new InvalidDataException(GetErrorMessage($"A valid Territory name"));
                initializer.AddTerritoryNeighbor(territoryName, neighborName);
            }
        }
    }
}
