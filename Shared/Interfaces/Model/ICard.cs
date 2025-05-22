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
/// Default serialization methods provided use reflection and should be overridden if there are performance concerns.
/// </remarks>
public interface ICard : IBinarySerializable
{
    #region Properties
    /// <summary>
    /// Gets or sets the logger.
    /// </summary>
    ILogger Logger { get; set; }
    /// <summary>
    /// Contains the name of each property that should be serialized via <see cref="IBinarySerializable.GetBinarySerials"/>. 
    /// </summary>
    /// <value>
    /// Names should match 'nameof(Property)', and the property must be an IConvertible or IEnumberable{IConvertible} for work with <see cref="BinarySerializer"/>.
    /// </value>
    HashSet<string> SerializablePropertyNames { get; }
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
    #endregion
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
            serialData.Add(new SerializedData(typeof(int), SerializablePropertyNames.Count, instanceType.Name));
            foreach (PropertyInfo propInfo in orderedProperties) {
                string propName = propInfo.Name;
                Type propType = propInfo.PropertyType;
                if (!SerializablePropertyNames.Contains(propName))
                    continue;
                if (!BinarySerializer.IsSerializable(propType)) {
                    Logger.LogWarning("{Card} attempted to serialize a property, {Name}, that was not serializable.", this, propName);
                    continue;
                }
                if (propInfo.GetValue(this) is not object propValue)
                    continue;
                IConvertible[] propConvertibles = new IConvertible[1];
                if (typeof(IEnumerable).IsAssignableFrom(propType) && propInfo.PropertyType != typeof(string)) {
                    propConvertibles = BinarySerializer.ToIConvertibleCollection((IEnumerable)propValue);
                }
                else
                    propConvertibles[0] = (IConvertible)propValue;

                serialData.Add(new SerializedData(typeof(int), propConvertibles.Length));
                serialData.Add(new SerializedData(propType, [.. propConvertibles], propInfo.Name)); // Name is used by SerialPropertyTypeMap
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

            if (loadedNumProperties != SerializablePropertyNames.Count)
                return false;
            if (cardProps.Select(prop => prop.Name) is not IEnumerable<string> propertyNames)
                return false;
            if (propertyNames.Intersect(SerializablePropertyNames) is not IEnumerable<string> matchingNames)
                return false;
            if (matchingNames.OrderBy(name => name).ToHashSet() is not HashSet<string> orderedMatchNames)
                return false;


            foreach (string propName in orderedMatchNames) {
                int numValues = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
                string readPropName = reader.ReadString();
                if (readPropName != propName) {
                    Logger.LogError("{Card} attempted to load from binary, but there was a property name mismatch.", this);
                    return false;
                }
                if (cardProps.Where(prop => prop.Name == readPropName).FirstOrDefault() is not PropertyInfo matchingProperty) {
                    Logger.LogError("{Card} attempted to load from binary, but the name of a loaded property, {name}, was not found.", this, readPropName);
                    return false;
                }

                Type propType = matchingProperty.PropertyType;
                if (!propType.IsArray) {
                    matchingProperty.SetValue(this, BinarySerializer.ReadConvertible(reader, propType));
                }
                else {
                    if (propType.GetElementType() is not Type elementType) {
                        Logger.LogError("{Card} attempted to load an array property, {name}, from binary, but failed to get its member type.", this, propName);
                        return false;
                    }
                    if (elementType.IsEnum)
                        matchingProperty.SetValue(this, BinarySerializer.ReadEnums(reader, elementType, numValues));
                    else if (elementType == typeof(string))
                        matchingProperty.SetValue(this, BinarySerializer.ReadStrings(reader, numValues));
                    else
                        matchingProperty.SetValue(this, BinarySerializer.ReadConvertibles(reader, elementType, numValues));
                }
            }
        } catch (Exception ex) {
            Logger.LogError("An exception was thrown while loading {Card}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
}
