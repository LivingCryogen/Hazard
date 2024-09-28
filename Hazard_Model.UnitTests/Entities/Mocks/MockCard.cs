using Hazard_Model.Tests.Fixtures.Mocks;
using Hazard_Model.Tests.Fixtures.Stubs;
using Hazard_Share.Enums;
using Hazard_Share.Interfaces.Model;
using Microsoft.Extensions.Logging;

namespace Hazard_Model.Tests.Entities.Mocks;

public class MockCard : ITroopCard
{
    public enum Insignia
    {
        Null = -1,
        Marine = 0,
        FighterJet = 1,
        Tank = 2
    }

    public MockCard()
    {
        ID = Guid.NewGuid();
        IsTradeable = true;
        CardSet = new MockCardSet();
        ParentTypeName = string.Empty;
    }
    public MockCard(ICardSet cardSet)
    {
        CardSet = cardSet;
        ID = Guid.NewGuid();
        IsTradeable = true;
        ParentTypeName = cardSet.GetType().Name;
    }
    public string TypeName { get; } = nameof(MockCard);
    public string ParentTypeName { get; private set; }
    public Insignia Insigne { get; set; }
    Enum ITroopCard.Insigne { get => Insigne; set { Insigne = (Insignia)Convert.ToInt32(value); } }

    public Dictionary<string, Type> PropertySerializableTypeMap { get; } = new()
    {
        { nameof(ID), typeof(string) },
        { nameof(Target), typeof(int) },
        { nameof(Insigne), typeof(int) },
        { nameof(ParentTypeName), typeof(string) },
        { nameof(IsTradeable), typeof(bool) }
    };

    public ILogger Logger { get; } = new LoggerStubT<MockCard>();
    public Guid ID { get; set; }
    public ICardSet? CardSet { get; set; }
    public MockTerrID[] Target { get; set; } = [];
    TerrID[] ICard.Target { get => Target.Select(item => (TerrID)(int)item).ToArray(); set { Target = value.Select(item => (MockTerrID)(int)item).ToArray(); } }
    public bool IsTradeable { get; set; }

    public bool InitializePropertyFromBinary(BinaryReader reader, string propName, int numValues)
    {
        if (propName == nameof(Insigne)) {
            Insigne = (Insignia)reader.ReadInt32();
            return true;
        }
        else if (propName == nameof(Target)) {
            if (numValues == 0) {
                Target = [];
                return true;
            }
            else {
                Target = new MockTerrID[numValues];
                for (int i = 0; i < numValues; i++)
                    Target[i] = (MockTerrID)reader.ReadInt32();

                return true;
            }
        }
        else if (propName == nameof(ID)) {
            if (numValues == 1) {
                ID = Guid.Parse(reader.ReadString());
                return true;
            }
        }
        else if (propName == nameof(ParentTypeName)) {
            ParentTypeName = reader.ReadString();
            return true;
        }
        else if (propName == nameof(IsTradeable)) {
            IsTradeable = reader.ReadBoolean();
            return true;
        }
        return false;
    }


}
