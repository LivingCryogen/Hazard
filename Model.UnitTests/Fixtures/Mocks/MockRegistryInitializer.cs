using Model.Tests.DataAccess.Mocks;
using Model.Tests.Entities.Mocks;
using Shared.Services.Registry;

namespace Model.Tests.Fixtures.Mocks;

public class MockRegistryInitializer : IRegistryInitializer
{
    private struct CardRegistryRecord
    {
        public Type CardType;
        public Type CardSetType;
        public string Name;
        public string SetName;
    }

    private struct CardSetRegistryRecord
    {
        public Type CardSetType;
        public string Name;
        public Type CardType;
        public string DataFileName;
        public MockCardDataJConverter CardSetDataConverter;
        public Type ConvertedDataType;
    }

    private readonly CardRegistryRecord[] _cardTypeRegistryRecords =
    [
        new()
        {
            CardType = typeof(MockCard),
            Name = nameof(MockCard),
            CardSetType = typeof(MockCardSet),
            SetName = nameof(MockCardSet)
        }
    ];

    private readonly CardSetRegistryRecord[] _cardSetTypeRegistryRecords =
    [
        new()
        {
            CardSetType = typeof(MockCardSet),
            Name = nameof(MockCardSet),
            CardType = typeof(MockCard),
            DataFileName = Path.Combine("Assets", "Cards", nameof(MockCard) + "Set.json"),
            CardSetDataConverter = new MockCardDataJConverter(),
            ConvertedDataType = typeof(MockCardSet) // The MockCardSetDataJConverter returns an instance of MockCardSet because the converter partially initializes it, providing 'JData'; final initialization is performed by the AssetFactory.
        }
    ];

    public void PopulateRegistry(ITypeRegister<ITypeRelations> registry)
    {
        foreach (var registryInfo in _cardTypeRegistryRecords)
        {
            TypeRelations cardRelations = new();
            cardRelations.Add(registryInfo.Name, RegistryRelation.Name);
            cardRelations.Add(registryInfo.CardSetType, RegistryRelation.CollectionType);
            cardRelations.Add(registryInfo.SetName, RegistryRelation.CollectionName);
            registry.Register(registryInfo.CardType, cardRelations);
        }

        foreach (var registryInfo in _cardSetTypeRegistryRecords)
        {
            TypeRelations cardSetRelations = new();
            cardSetRelations.Add(registryInfo.Name, RegistryRelation.Name);
            cardSetRelations.Add(registryInfo.CardType, RegistryRelation.ElementType);
            cardSetRelations.Add(registryInfo.DataFileName, RegistryRelation.DataFileName);
            cardSetRelations.Add(registryInfo.CardSetDataConverter, RegistryRelation.DataConverter);
            cardSetRelations.Add(registryInfo.ConvertedDataType, RegistryRelation.ConvertedDataType);
            registry.Register(registryInfo.CardSetType, cardSetRelations);
        }
    }
}
