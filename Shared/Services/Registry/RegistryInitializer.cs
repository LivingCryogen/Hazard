using Microsoft.Extensions.Options;
using Model.DataAccess;
using Model.DataAccess.Cards;
using Model.Entities.Cards;
using Shared.Geography;
using Shared.Interfaces.Model;
using Shared.Services.Options;
using System.Text.Json.Serialization;

namespace Shared.Services.Registry;

/** <inheritdoc cref="IRegistryInitializer"/>
 * <see cref="CardRegistryRecord"/> and <see cref="CardSetRegistryRecord"/> provides a stable pattern for adding more <see cref="$1ICard{T}$2"/> types in the future.
 * For now we work only with the default, base set of <see cref="$1ICard{T}$2"/>s implemented by <see cref="TroopCard"/> and <see cref="TroopCardSet"/>.*/
public class RegistryInitializer(IOptions<AppConfig> options) : IRegistryInitializer
{
    private string _geographyFilePath = options.Value.DataFileMap[nameof(BoardGeography) + ".json"];

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
        public JsonConverter CardSetDataConverter;
        public Type ConvertedDataType;
    }

    private readonly CardRegistryRecord[] _cardTypeRegistryRecords =
        [
            new() {
                CardType = typeof(TroopCard),
                Name = nameof(TroopCard),
                CardSetType = typeof(TroopCardSet),
                SetName = nameof(TroopCardSet)
            }
        ];
    private readonly CardSetRegistryRecord[] _cardSetTypeRegistryRecords =
        [
            new()
            {
                CardSetType = typeof(TroopCardSet),
                Name = nameof(TroopCardSet),
                CardType = typeof(TroopCard),
                DataFileName = options.Value.DataFileMap[nameof(TroopCard) + "Set.json"],
                CardSetDataConverter = new TroopCardSetDataJConverter(),
                ConvertedDataType = typeof(TroopCardSet) // The TroopCardSetDataJConverter returns an instance of TroopCardSet because the converter partially initializes it, providing 'JData'; final initialization is performed by the AssetFactory.
            }
        ];

    /** <inheritdoc cref="IRegistryInitializer"/>
     * Extending the program to initialize with data from files other that those for <see cref="$1ICard{T}$2"/>s will
     * require adding additional structs/records and population logic. */
    public void PopulateRegistry(ITypeRegister<ITypeRelations> registry)
    {
        RegisterGeography(registry);

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

    private void RegisterGeography(ITypeRegister<ITypeRelations> registry)
    {
        TypeRelations geographyRelations = new();
        geographyRelations.Add(nameof(BoardGeography), RegistryRelation.Name);
        geographyRelations.Add(_geographyFilePath, RegistryRelation.DataFileName);
        geographyRelations.Add(typeof(GeographyInitializer), RegistryRelation.ConvertedDataType);
        geographyRelations.Add(new GeographyJConverter(), RegistryRelation.DataConverter);
        registry.Register(typeof(BoardGeography), geographyRelations);
    }
}