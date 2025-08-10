using Microsoft.Extensions.Logging;
using Model.Tests.Fixtures.Mocks;
using Model.Tests.Fixtures.Stubs;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;

namespace Model.Tests.Entities.Mocks;

public class MockCard(ICardSet cardSet) : ITroopCard
{
    private readonly ILogger _logger = new LoggerStubT<MockCard>();
    public enum Insignia
    {
        Null = -1,
        Marine = 0,
        FighterJet = 1,
        Tank = 2
    }

    public string TypeName { get; set; } = nameof(MockCard);
    public string ParentTypeName { get; private set; } = cardSet.GetType().Name;
    public Insignia Insigne { get; set; }
    Enum ITroopCard.Insigne { get => Insigne; set { Insigne = (Insignia)Convert.ToInt32(value); } }

    public HashSet<string> SerializablePropertyNames { get; } = [
        nameof(ID),
        nameof(Target),
        nameof(Insigne),
        nameof(ParentTypeName),
        nameof(IsTradeable),
        nameof(TestInts),
        nameof(TestBools),
        nameof(TestLongs),
        nameof(TestBytes),
        nameof(TestStrings),
        ];

    public string ID { get; set; } = Guid.NewGuid().ToString();
    public ILogger Logger { get; set; } = new LoggerStubT<MockCard>();
    public ICardSet? CardSet { get; set; } = cardSet;
    public TerrID[] Target { get; set; } = [];
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
