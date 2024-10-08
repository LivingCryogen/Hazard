﻿using Hazard_Model.Entities.Cards;
using Hazard_Share.Enums;
using Hazard_Share.Interfaces.Model;
using Hazard_Share.Services.Registry;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hazard_Model.DataAccess.Cards;

/// <summary>
/// The base card set '.json' converter. 
/// </summary>
/// <remarks>
/// 
/// </remarks>
public class TroopCardSetDataJConverter : JsonConverter<TroopCardSet>, ICardSetDataJConverter
{
    /// <inheritdoc cref="ICardSetDataJConverter.ReadCardSetData(string)"/>
    public ICardSet? ReadCardSetData(string registeredFileName)
    {
        ReadOnlySpan<byte> jsonROSpan = File.ReadAllBytes(registeredFileName);
        var reader = new Utf8JsonReader(jsonROSpan);

        return Read(ref reader, typeof(TroopCardSet), options: JsonSerializerOptions.Default);
    }
    /// <summary>
    /// Reads a CardSet .json into a <see cref="TroopCardSet"/> object. 
    /// </summary>
    /// <remarks>
    /// Overrides <see cref="JsonConverter{T}.Read(ref Utf8JsonReader, Type, JsonSerializerOptions)"/>. 
    /// </remarks>
    /// <param name="reader">A <see cref="Utf8JsonReader"/> provided the <see langword="byte"/>s of a '.json' data file registered in a <see cref="TypeRegister"/>.</param>
    /// <param name="typeToConvert">The target conversion <see cref="Type"/>, usually marked by <see cref="RegistryRelation.ConvertedDataType"/> in a <see cref="TypeRegister"/>.</param>
    /// <param name="options">Should be <see cref="JsonSerializerOptions.Default"/>.</param>
    /// <returns>A <see cref="TroopCardSetData"/> which <see cref="Hazard_Share.Interfaces.Model.IAssetFactory"/> will use to build <see cref="TroopCard"/>s.</returns>
    public override TroopCardSet? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        TroopCardSet? newTroopCardSet = new() { JData = new TroopCardSetData() };
        while (reader.Read()) {
            JsonTokenType tokenType = reader.TokenType;

            switch (tokenType) {
                case JsonTokenType.PropertyName:
                    if (reader.ValueTextEquals("TroopCards")) {
                        reader.Read(); // move to value

                        if (reader.TokenType != JsonTokenType.StartArray)
                            throw new JsonException($"TroopCardSetData converter expects an Array for value of 'TroopCards' property but the first token in the property value was not a StartArray token.");

                        List<TerrID[]> cardSetTargets = [];
                        List<TroopInsignia> cardSetInsignia = [];

                        while (reader.Read()) { // Loops through the "TroopCards" Property -- the list of TroopCardData to add
                            if (reader.TokenType == JsonTokenType.EndArray)
                                break;

                            if (reader.TokenType == JsonTokenType.PropertyName) {
                                if (reader.ValueTextEquals("Targets")) {

                                    reader.Read(); // move to value

                                    if (reader.TokenType != JsonTokenType.StartArray)
                                        throw new JsonException($"TroopCardData expects an Array for value of 'Targets' property but the first token in the property value was not a StartArray token.");

                                    List<string> targetStrings = [];
                                    while (reader.Read()) { // Loops through the "Targets" Array
                                        if (reader.TokenType == JsonTokenType.EndArray)
                                            break;

                                        targetStrings.Add(reader.GetString() ?? string.Empty);
                                    }
                                    var targetsWithoutBlanks = from target in targetStrings
                                                               select target.Replace(" ", "");

                                    var targetIDs = from target in targetsWithoutBlanks
                                                    select Enum.Parse<TerrID>(target, false);

                                    cardSetTargets.Add(targetIDs.ToArray());
                                }
                                else if (reader.ValueTextEquals("Insignia")) {
                                    reader.Read(); // move to value

                                    string insigniaString = reader.GetString() ?? string.Empty;
                                    TroopInsignia cardInsigne = Enum.Parse<TroopInsignia>(insigniaString, false);
                                    cardSetInsignia.Add(cardInsigne);
                                }
                            }
                        }

                        newTroopCardSet.JData.Targets = [.. cardSetTargets];
                        newTroopCardSet.JData.Insignia = [.. cardSetInsignia];
                    }
                    break;
            }
        }

        return newTroopCardSet;
    }

    /// Since 'Save' functions use Binary, there is no current need for ICard .json writing.
    public override void Write(Utf8JsonWriter writer, TroopCardSet value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
