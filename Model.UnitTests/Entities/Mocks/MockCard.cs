using Microsoft.Extensions.Logging;
using Model.Tests.Fixtures.Mocks;
using Model.Tests.Fixtures.Stubs;
using Shared.Enums;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;

namespace Model.Tests.Entities.Mocks;

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
    }
    public MockCard(ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger<MockCard>();
    }
    public MockCard(ILogger<MockCard> logger)
    {
        IsTradeable = true;
        Logger = logger;
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
        { nameof(IsTradeable), typeof(bool) },
        { nameof(TestInts), typeof(int) },
        { nameof(TestBools), typeof(bool) },
        { nameof(TestLongs), typeof(long) },
        { nameof(TestBytes), typeof(byte) },
        { nameof(TestStrings), typeof(string)}
    };
    public string ID { get; set; } = Guid.NewGuid().ToString();
    public ILogger Logger { get; set; } = new LoggerStubT<MockCard>();
    public ICardSet? CardSet { get; set; }
    public MockTerrID[] Target { get; set; } = [];
    TerrID[] ICard.Target { get => Target.Select(item => (TerrID)(int)item).ToArray(); set { Target = value.Select(item => (MockTerrID)(int)item).ToArray(); } }
    public bool IsTradeable { get; set; } = true;
    public int[] TestInts { get; set; } = [];
    public bool[] TestBools { get; set; } = [];
    public long[] TestLongs { get; set; } = [];
    public byte[] TestBytes { get; set; } = [];
    public string[] TestStrings { get; set; } = [];

    public void FillTestValues()
    {
        TestInts = [1, 3, 5];
        TestBools = [true, false];
        TestLongs = [1678359, 32482859, 5244245];
        TestBytes = [new byte(), new byte()];
        TestStrings = ["Muad", "Dib"];
    }
}
