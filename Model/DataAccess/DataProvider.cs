using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model.DataAccess.Cards;
using Shared.Geography;
using Shared.Interfaces.Model;
using Shared.Services.Options;
using Shared.Services.Registry;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Model.DataAccess;
/// <inheritdoc cref="IDataProvider"/>
public class DataProvider(ITypeRegister<ITypeRelations> typeRegister, ILogger<DataProvider> logger, IOptions<AppConfig> appConfig) : IDataProvider
{
    private readonly ITypeRegister<ITypeRelations> _typeRegister = typeRegister;
    private readonly ILogger _logger = logger;

    /// <inheritdoc cref="IDataProvider.DataFileMap"/>s
    public ReadOnlyDictionary<string, string> DataFileMap { get; } = new(appConfig.Value.DataFileMap);

    private bool CanGetDataFromParent(Type memberType, out Type? parentType, out ITypeRelations? parentRelations)
    {
        if (!_typeRegister.TryGetParentType(memberType, out Type? parent) || parent == null)
        {
            parentType = null;
            parentRelations = null;
            return false;
        }
        if (_typeRegister[parent] is not ITypeRelations collectionRelata)
        {
            parentType = parent;
            parentRelations = null;
            return false;
        }
        if (collectionRelata[RegistryRelation.DataFileName] == null)
        {
            parentType = parent;
            parentRelations = collectionRelata;
            return false;
        }
        if (collectionRelata[RegistryRelation.DataConverter] == null)
        {
            parentType = parent;
            parentRelations = collectionRelata;
            return false;
        }
        parentType = parent;
        parentRelations = collectionRelata;
        return true;
    }
    /** <inheritdoc cref="IDataProvider.GetData(string)"/>
     * <exception cref="KeyNotFoundException">Thrown when any necessary element is not found in the <see cref="TypeRegister"/> or when, in the case of data file names, there is conflict between
     * the registered name and the name found in <see cref="DataFileMap"/>.</exception>
     * Like <see cref="AssetFactory"/>, any expansion or extension of assets requires an extension here. */
    public object? GetData(string typeName)
    {
        if (_typeRegister[typeName] is not Type registeredType)
            return null;
        if (_typeRegister[registeredType] is not ITypeRelations registeredRelations)
            return null;

        // If a registered Type has no associated DataFile, but it is a Type of a Collection which DOES have a DataFile, use the Collection Type instead
        if (registeredRelations[RegistryRelation.DataFileName] == null &&
            CanGetDataFromParent(registeredType, out Type? parentType, out ITypeRelations? parentRelations) &&
            parentType != null &&
            parentRelations != null)
        {
            registeredRelations = parentRelations;
            registeredType = parentType;
        }

        if (registeredRelations[RegistryRelation.DataFileName] is not string registeredFileName)
            throw new KeyNotFoundException($"No DataFileName was found in registry for {nameof(registeredType)}");

        string fileName = Path.GetFileName(registeredFileName);
        if (!DataFileMap.TryGetValue(fileName, out string? filePath))
            throw new KeyNotFoundException($"{registeredFileName} was registered for {nameof(registeredType)}, but was not as a key in the data file map provided via AppConfig.");
        if (filePath == null)
            throw new InvalidDataException($"{registeredFileName} was registered for {nameof(registeredType)}, and {fileName} as a key in the data file map, but its value was null.");

        object? converter = registeredRelations[RegistryRelation.DataConverter] ?? throw new KeyNotFoundException($"No Converter was registered for {nameof(registeredType)}.");

        Type? targetType = registeredRelations[RegistryRelation.ConvertedDataType] as Type;
        if (targetType != null)
            return ReadData(converter, targetType, filePath);

        if (converter is JsonConverter && fileName.EndsWith(".json") && fileName.Contains(typeName))
            targetType = registeredType;
        else
            throw new KeyNotFoundException($"No target Conversion Type for converter {nameof(converter)} was registered for {nameof(registeredType)}, and default converter requirements were not met for registered type {nameof(registeredType)}.");

        return ReadData(converter, targetType, filePath);
    }
    private object? ReadData(object converter, Type conversionTargetType, string filePath)
    {
        if (converter is ICardSetDataJConverter jConverter)
        {
            if (!typeof(ICardSetData).IsAssignableFrom(conversionTargetType) && !typeof(ICardSet).IsAssignableFrom(conversionTargetType))
                throw new ArgumentException($"The provided target Type is not valid. Converter {converter} requires a target Type which implements ICardSetData or ICardSet.");

            try
            {
                return jConverter.ReadCardSetData(filePath);
            }
            catch (Exception e)
            {
                _logger.LogError("{Converter} encountered an exception attempting to read card set data: {Message}", jConverter, e.Message);
                return null;
            }
        }
        if (converter is GeographyJConverter geographyConverter)
        {
            try
            {
                ReadOnlySpan<byte> jsonROSpan = File.ReadAllBytes(filePath);
                var reader = new Utf8JsonReader(jsonROSpan);
                return geographyConverter.Read(ref reader, typeof(GeographyInitializer), options: JsonSerializerOptions.Default) ??
                    throw new InvalidDataException($"{geographyConverter} failed to return a valid {nameof(GeographyInitializer)}.");
            }
            catch (Exception e)
            {
                _logger.LogError("{Converter} encountered an exception attempting to read Geography data: {Message}", geographyConverter, e.Message);
                return null;
            }
        }

        // Later extensions would get appended here
        return null;
    }
}
