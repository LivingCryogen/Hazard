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
        IsTradeable = true;
    }
    public MockCard(ICardSet cardSet)
    {
        CardSet = cardSet;
        IsTradeable = true;
        ParentTypeName = cardSet.GetType().Name;
    }
    public string TypeName { get; set; } = nameof(MockCard);
    public string ParentTypeName { get; private set; } = nameof(MockCardSet);
    public Insignia Insigne { get; set; }
    Enum ITroopCard.Insigne { get => Insigne; set { Insigne = (Insignia)Convert.ToInt32(value); } }

    public Dictionary<string, Type> PropertySerializableTypeMap { get; } = new()
    {
        { nameof(ID), typeof(string) },
        { nameof(Target), typeof(MockTerrID) },
        { nameof(Insigne), typeof(Insignia) },
        { nameof(ParentTypeName), typeof(string) },
        { nameof(IsTradeable), typeof(bool) }
    };
    public string ID { get; set; } = Guid.NewGuid().ToString();
    public ILogger Logger { get; } = new LoggerStubT<MockCard>();
    public ICardSet? CardSet { get; set; }
    public MockTerrID[] Target { get; set; } = [MockTerrID.Delaware];
    TerrID[] ICard.Target { get => Target.Select(item => (TerrID)(int)item).ToArray(); set { Target = value.Select(item => (MockTerrID)(int)item).ToArray(); } }
    public bool IsTradeable { get; set; }
}
