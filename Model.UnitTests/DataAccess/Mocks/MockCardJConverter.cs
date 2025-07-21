using Model.DataAccess.Cards;
using Model.Tests.Entities.Mocks;
using Model.Tests.Fixtures;
using Model.Tests.Fixtures.Mocks;
using Shared.Interfaces.Model;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Model.Tests.DataAccess.Mocks;

public class MockCardDataJConverter : JsonConverter<MockCardSet>, ICardSetDataJConverter<MockTerrID>
{
    public ICardSet<MockTerrID>? ReadCardSetData(string registeredFileName)
    {
        ReadOnlySpan<byte> jsonROSpan = FileProcessor.ReadAllBytes(registeredFileName);
        var reader = new Utf8JsonReader(jsonROSpan);

        return Read(ref reader, typeof(MockCardSet), options: JsonSerializerOptions.Default);
    }
    public override MockCardSet? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        MockCardSet? newMockCardSet = new() { JData = new MockCardSetData() };
        while (reader.Read())
        {
            JsonTokenType tokenType = reader.TokenType;

            switch (tokenType)
            {
                case JsonTokenType.PropertyName:
                    if (reader.ValueTextEquals("TroopCards"))
                    {
                        reader.Read(); // move to value

                        if (reader.TokenType != JsonTokenType.StartArray)
                            throw new JsonException($"TroopCardSetData converter expects an Array for value of 'TroopCards' property but the first token in the property value was not a StartArray token.");

                        List<MockTerrID[]> cardSetTargets = [];
                        List<MockCard.Insignia> cardSetInsignia = [];

                        while (reader.Read())
                        { // Loops through the "TroopCards" Property -- the list of TroopCardData to add
                            if (reader.TokenType == JsonTokenType.EndArray)
                                break;

                            if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                if (reader.ValueTextEquals("Targets"))
                                {

                                    reader.Read(); // move to value

                                    if (reader.TokenType != JsonTokenType.StartArray)
                                        throw new JsonException($"TroopCardData expects an Array for value of 'Targets' property but the first token in the property value was not a StartArray token.");

                                    List<string> targetStrings = [];
                                    while (reader.Read())
                                    { // Loops through the "Targets" Array
                                        if (reader.TokenType == JsonTokenType.EndArray)
                                            break;

                                        targetStrings.Add(reader.GetString() ?? string.Empty);
                                    }
                                    var targetsWithoutBlanks = from target in targetStrings
                                                               select target.Replace(" ", "");

                                    var targetIDs = from target in targetsWithoutBlanks
                                                    select Enum.Parse<MockTerrID>(target, false);


                                    cardSetTargets.Add(targetIDs.ToArray());
                                }
                                else if (reader.ValueTextEquals("Insignia"))
                                {
                                    reader.Read(); // move to value

                                    string? insigniaString = reader.GetString();
                                    MockCard.Insignia cardInsigne = Enum.Parse<MockCard.Insignia>(insigniaString ?? "Null", false);
                                    cardSetInsignia.Add(cardInsigne);
                                }
                            }
                        }
                        // MockTerrID must be cast to the Interface's defined type of TerrID -- recall there is not perfect overlap
                        ((MockCardSetData)newMockCardSet.JData).Targets = [.. cardSetTargets];
                        ((MockCardSetData)newMockCardSet.JData).Insignia = [.. cardSetInsignia];
                    }
                    break;
            }
        }

        return newMockCardSet;
    }

    public override void Write(Utf8JsonWriter writer, MockCardSet value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
