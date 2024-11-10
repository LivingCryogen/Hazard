using Microsoft.Extensions.Logging;
using Shared.Geography.Enums;
using Shared.Services.Serializer;
using System.Collections;
using System.Reflection;

namespace Shared.Interfaces.Model;

/// <summary>
/// Base interface for all cards. Provides default properties and methods allowing for run-time serialization. 
/// </summary>
/// <remarks>
/// Default serialization methods provided for <see cref="IBinarySerializable.GetBinarySerials()"/>, and internally <see cref="TryGetConvertibles(PropertyInfo, out IConvertible[])"/>,<br/> use reflection and should be overridden if there are performance concerns.
/// </remarks>
public interface ICard : IBinarySerializable
{
    /// <summary>
    /// Gets or sets the logger.
    /// </summary>
    ILogger Logger { get; set; }
    /// <summary>
    /// Maps the name of each serializable property to its type. 
    /// </summary>
    /// <value>
    /// Type values must be IConvertibles or IEnumberable{IConvertible} for work with <see cref="BinarySerializer"/>.
    /// </value>
    /// <remarks>
    /// The map is used for binary deserialization by <see cref="BinarySerializer"/>.
    /// </remarks>
    Dictionary<string, Type> PropertySerializableTypeMap { get; }
    /// <summary>
    /// The name of this card's type.
    /// </summary>
    /// <remarks>
    /// Serves as a cached value that allows us to avoid multiple reflection method calls. 
    /// </remarks>
    string TypeName { get; set; }
    /// <summary>
    /// Gets the name of the parent type of this card.
    /// </summary>
    /// <remarks>
    /// Should be set by default to the <see langword="nameof"/> the type of the <see cref="ICardSet"/> implementation containing this card (its "parent").
    /// </remarks>
    string ParentTypeName { get; }
    /// <summary>
    /// Gets or sets a reference to a parent <see cref="ICardSet"/> containing this card.
    /// </summary>
    /// <value>
    /// The <see cref="ICardSet"/> containing this <see cref="ICard"/>, if both have been initialized and mapped. Otherwise, <see langword="null"/>.
    /// </value>
    ICardSet? CardSet { get; set; }
    /// <summary>
    /// Gets or sets a list of this card's territory 'targets'.
    /// </summary>
    TerrID[] Target { get; set; }
    /// <summary>
    /// Gets a flag indicating this card can be traded in for additional armies.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this card can be traded in. Otherwise, <see langword="false"/>.
    /// </value>
    bool IsTradeable { get; set; }
    /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>
    async Task<SerializedData[]> IBinarySerializable.GetBinarySerials()
    {
        return await Task.Run(() =>
        {
            Type instanceType = this.GetType();
            PropertyInfo[] instanceProperties = instanceType.GetProperties();
            var orderedProperties = instanceProperties.OrderBy(property => property.Name);

            List<SerializedData> serialData = [];
            // The Name tag must be read on the other end with BinaryReader.ReadString()
            serialData.Add(new SerializedData(typeof(int), [PropertySerializableTypeMap.Count], instanceType.Name)); 
            foreach (PropertyInfo propInfo in orderedProperties) {
                if (!PropertySerializableTypeMap.TryGetValue(propInfo.Name, out Type? mappedType) || mappedType is null || 
                    !BinarySerializer.IsSerializable(mappedType)) {
                    Logger.LogWarning("", );
                    continue;
                }
                if (propInfo.GetValue(this) is not object propValue)
                    continue;
                IConvertible[] propConvertibles = new IConvertible[1];
                if (typeof(IEnumerable).IsAssignableFrom(mappedType) && mappedType != typeof(string)) {
                    propConvertibles = BinarySerializer.ToIConvertibleCollection((IEnumerable)propValue);
                }
                else
                    propConvertibles[0] = (IConvertible)propValue;

                serialData.Add(new SerializedData(typeof(int), [propConvertibles.Length]));
                serialData.Add(new SerializedData(mappedType, [.. propConvertibles], propInfo.Name)); // Name is used by SerialPropertyTypeMap
            }
            return serialData.ToArray();
        });
    }
    
    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
    bool IBinarySerializable.LoadFromBinary(BinaryReader reader)
    {
        // The first data point of GetBinarySerials is the Type Name of this card, but this is to be read outside of this Load method (e.g. in CardBase.LoadFromBinary())
        bool loadComplete = true;
        try {
            var cardProps = this.GetType().GetProperties();
            int loadedNumProperties = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            
            if (loadedNumProperties != PropertySerializableTypeMap.Count)
                return false;
            if (cardProps.Select(prop => prop.Name) is not IEnumerable<string> propertyNames)
                return false;
            // .OrderBy is necessary to ensure .Intersect does not scramble the order and lead to mismatches with the file being read MUST BE DONE ON SERIALIZED END!!
            var mappedPropNames = PropertySerializableTypeMap.Keys
                .Intersect(propertyNames)
                .OrderBy(name => name)
                .ToArray();
            if (mappedPropNames is not string[] orderedMappedNames)
                return false;

            for(int i = 0; i < orderedMappedNames.Length; i++) {
                int numValues = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                string readPropName = reader.ReadString();
                if (readPropName != orderedMappedNames[i]) {
                    Logger.LogError("{Card} attempted to load from binary, but there was a property name mismatch.", this);
                    return false;
                }
            }

            int numProperties = cardProps.Length;
            int targetLoadNum = numProperties - 4; // recall that CardSet, PropertySerializableTypeMap, TypeName, and Logger are excluded
            if (loadedNumProperties != targetLoadNum) {
                Logger.LogError("{Card} attempted to load from binary, but there was a property count mismatch.", this);
                return false;
            }
            if (PropertySerializableTypeMap.Keys.Count != targetLoadNum) {
                Logger.LogError("{Card} attempted to load from binary, but its {Map} count was incorrect. Ensure that each serializable property is registered.", this, PropertySerializableTypeMap);
                return false;
            }

            int propIndex = 0;
            while (propIndex < numProperties) {
                string propName = cardProps[propIndex].Name;
                if (propName == nameof(CardSet) || propName == nameof(PropertySerializableTypeMap) || propName == nameof(TypeName) || propName == nameof(Logger)) {
                    propIndex++;
                    continue;
                }

                int numValsLoaded = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                string loadedName = reader.ReadString();
                if (propName != loadedName) {
                    Logger.LogError("{Card} attempted to load from binary, but there was a property name mismatch.", this);
                    return false;
                }

                Type propType = cardProps[propIndex].PropertyType;
                if (PropertySerializableTypeMap[loadedName] is not Type serialType) {
                    Logger.LogError("{Card} attempted to load from binary, but the name of a loaded property was not found in {Map}.", this, PropertySerializableTypeMap);
                    return false;
                }
                if (!propType.IsArray) {
                    if (numValsLoaded > 1) {
                        Logger.LogError("{Card} attempted to load from binary, but there was a property type mismatch: the property was not an array, but it attempted to load multiple values.", this);
                        return false;
                    }
                    cardProps[propIndex].SetValue(this, BinarySerializer.ReadConvertible(reader, serialType));
                }
                else {
                    if (propType.GetElementType() is not Type elementType) {
                        Logger.LogError("{Card} attempted to load an array property, {name}, from binary, but failed to get its member type.", this, propName);
                        return false;
                    }
                    if (elementType.IsEnum)
                        cardProps[propIndex].SetValue(this, BinarySerializer.ReadEnums(reader, serialType, numValsLoaded));
                    else if (elementType == typeof(string))
                        cardProps[propIndex].SetValue(this, BinarySerializer.ReadStrings(reader, numValsLoaded));
                    else
                        cardProps[propIndex].SetValue(this, BinarySerializer.ReadConvertibles(reader, serialType, numValsLoaded));
                }

                propIndex++;
            }
        } catch (Exception ex) {
            Logger.LogError("An exception was thrown while loading {Card}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
}
