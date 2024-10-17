using Microsoft.Extensions.Logging;
using Model.DataAccess.Cards;
using Share.Interfaces.Model;
using Share.Services.Registry;
using System.Text.Json.Serialization;

namespace Model.DataAccess;
/** <inheritdoc cref="IDataProvider"/>
 * 
 */
public class DataProvider(string[] dataFileNames, ITypeRegister<ITypeRelations> typeRegister, ILogger<DataProvider> logger) : IDataProvider
{
    private readonly ITypeRegister<ITypeRelations> _typeRegister = typeRegister;
    private readonly ILogger _logger = logger;

    /// <inheritdoc cref="IDataProvider.DataFileNames"/>s
    public string[] DataFileNames { get; } = dataFileNames;
    /** <inheritdoc cref="IDataProvider.GetData(string)"/>
     * <exception cref="KeyNotFoundException">Thrown when any necessary element is not found in the <see cref="TypeRegister"/> or when, in the case of data file names, there is conflict between
     * the registered name and the name found in <see cref="DataFileNames"/>.</exception>
     * Like <see cref="AssetFactory"/>, any expansion or extentsion of assets requires an extension here. */
    public object? GetData(string typeName)
    {
        Type? type = _typeRegister[typeName];
        if (type == null)
            return null;

        var typeRelata = _typeRegister[type];
        if (typeRelata == null)
            return null;

        // If a registered Type has no associated DataFile, but it is a Type of a Collection which DOES have a DataFile, use the Collection Type instead
        Type? collectionType = (Type?)typeRelata[RegistryRelation.CollectionType];
        if (collectionType != null) {
            var collectionRelata = _typeRegister[collectionType];
            if (collectionRelata != null) {
                if (collectionRelata[RegistryRelation.DataFileName] != null && typeRelata[RegistryRelation.DataFileName] == null && (Type?)collectionRelata[RegistryRelation.ElementType] == type) {
                    typeRelata = collectionRelata;
                    type = collectionType;
                }
            }
        }

        var registeredFileName = typeRelata![RegistryRelation.DataFileName] ?? throw new KeyNotFoundException($"No DataFileName was found in registry for {nameof(type)}");

        string fileName = (string)registeredFileName;
        if (!DataFileNames.Contains(fileName))
            throw new KeyNotFoundException($"No DataFileName was found in configuration for {fileName}.");

        object? converter = typeRelata[RegistryRelation.DataConverter] ?? throw new KeyNotFoundException($"No Converter was registered for {nameof(type)}.");

        Type? targetType = typeRelata[RegistryRelation.ConvertedDataType] as Type;
        if (targetType == null) {
            if (converter is JsonConverter && fileName.EndsWith(".json") && fileName.Contains(typeName))
                targetType = type;
            else
                throw new KeyNotFoundException($"No target Conversion Type for converter {nameof(converter)} was registered for {nameof(type)}, and default converter requirements were not met for registered type {nameof(type)}.");
        }

        return ReadData(converter, targetType, fileName);
    }
    private object? ReadData(object converter, Type conversionTargetType, string registeredFileName)
    {
        if (converter is ICardSetDataJConverter jConverter) {
            if (!typeof(ICardSetData).IsAssignableFrom(conversionTargetType) && !typeof(ICardSet).IsAssignableFrom(conversionTargetType))
                throw new ArgumentException($"The provided target Type is not valid. Converter {converter} requires a target Type which implements ICardSetData or ICardSet.");

            try {
                var data = jConverter.ReadCardSetData(registeredFileName);
                if (data is null)
                    return null;
                else
                    return data;
            } catch (Exception e) {
                _logger.LogError("{Converter} encountered an exception attempting to read card set data: {Message}", jConverter, e.Message);
                return null;
            }
        }         // Later extensions would get appended here
        else
            return null;
    }
}
