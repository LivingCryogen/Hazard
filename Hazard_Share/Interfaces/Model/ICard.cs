using Hazard_Share.Enums;
using Hazard_Share.Services.Serializer;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Runtime.Serialization.Formatters;

namespace Hazard_Share.Interfaces.Model;

/// <summary>
/// Base interface for all cards. Provides default properties and methods allowing for run-time serialization and a stub for deserialization. 
/// </summary>
/// <remarks>
/// The default serialization methods <see cref="GetSaveData"/>, <see cref="TryConvertToSerial"/>, and <see cref="TryGetPropertySerials"/> <br/>
/// use reflection and should be overridden if there are performance concerns.
/// </remarks>
public interface ICard : IBinarySerializable
{
    ILogger Logger { get; }
    /// <summary>
    /// Gets a binary conversion <see cref="Type"/> for each property, by name, of the <see cref="ICard"/>. 
    /// </summary>
    /// <value>
    /// A map of property name <see cref="string"/>s to <see cref="Type"/>s.
    /// </value>
    /// <remarks>
    /// The map is used for binary serialization by <see cref="Hazard_Model.DataAccess.BinarySerializer.SerializeCardInfo"/>. If an <see cref="ICard"/> is to be serialized via this method, <br/>
    /// it must initialize <see cref="PropertySerializableTypeMap"/> before calls to <see cref="Hazard_Model.DataAccess.BinarySerializer.WriteData"/>.
    /// </remarks>
    Dictionary<string, Type> PropertySerializableTypeMap { get; }
    string TypeName { get; set; }
    /// <summary>
    /// Gets the name of the parent <see cref="Type"/> of this <see cref="ICard"/>. 
    /// </summary>
    /// <remarks>
    /// Should be set by default to the <see langword="nameof"/> the <see cref="Type"/> of the <see cref="ICardSet"/> implementation containing this <see cref="ICard"/> (its "parent").
    /// </remarks>
    /// <value>
    /// A string. 
    /// </value>
    string ParentTypeName { get; }
    /// <summary>
    /// Gets or sets a reference to a parent <see cref="ICardSet"/> containing this card.
    /// </summary>
    /// <value>
    /// The <see cref="ICardSet"/> containing this <see cref="ICard"/>, if both have been initialized and mapped. Otherwise, <see langword="null"/>.
    /// </value>
    ICardSet? CardSet { get; set; }
    /// <summary>
    /// Gets or sets a list of territories 'targeted' by this <see cref="ICard"/>.
    /// </summary>
    /// <value>
    /// An array of <see cref="TerrID"/> of any length; future <see cref="ICard"/> implementations that do not involve territories in any way can still be created by using an empty array.
    /// </value>
    TerrID[] Target { get; set; }
    /// <summary>
    /// Gets a flag indicating this <see cref="ICard"/> can be traded in for additional armies.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this <see cref="ICard"/> can be traded in. Otherwise, <see langword="false"/>.
    /// </value>
    bool IsTradeable { get; set; }

    async Task<SerializedData[]> IBinarySerializable.GetBinarySerials()
    {
        return await Task.Run(() =>
        {
            Type instanceType = this.GetType();
            PropertyInfo[] instanceProperties = instanceType.GetProperties();

            List<SerializedData> serialData = [];
            serialData.Add(new SerializedData(typeof(int), [instanceProperties.Length - 4], instanceType.Name)); // CardSet, SerialPropertyTypeMap, TypeName, Logger excluded.
            foreach (PropertyInfo propInfo in instanceProperties) {
                string propName = propInfo.Name;
                if (propName == nameof(CardSet) || propName == nameof(PropertySerializableTypeMap) || propName == nameof(TypeName) || propName == nameof(Logger))
                    continue;
                if (PropertySerializableTypeMap[propName] is not Type mappedType) {
                    Logger.LogWarning("{Card} binary serialization failed on {Property} because a corresponding Type was not found in {Map}.", this, propName, PropertySerializableTypeMap);
                    continue;
                }

                if (!TryGetConvertibles(propInfo, out IConvertible[] propConvertibles) || propConvertibles == null) {
                    Logger.LogWarning("{Card} binary serialization failed on {Property}.", this, propName);
                    continue;
                }
                serialData.Add(new SerializedData(typeof(int), [propConvertibles.Length]));
                serialData.Add(new SerializedData(mappedType, propConvertibles, propName)); // Name is used by SerialPropertyTypeMap
            }
            return serialData.ToArray();
        });
    }
    bool TryGetConvertibles(PropertyInfo propInfo, out IConvertible[] convertibles)
    {
        switch (propInfo.PropertyType) {
            case Type t when t == typeof(string):
                if (propInfo.GetValue(this) is not string stringValue) {
                    convertibles = [];
                    return false;
                }
                convertibles = [stringValue];
                return true;
            case Type t when typeof(IEnumerable).IsAssignableFrom(t) && t != typeof(string):
                if (propInfo.GetValue(this) is not IEnumerable propValue) {
                    convertibles = [];
                    return false;
                }
                if (!propValue.Cast<object>().Any()) {
                    Logger.LogWarning("{Card}'s property {Property} returned an empty enumerable on serialization.", this, propInfo.Name);
                    convertibles = [];
                    return true;
                }

                // check if all generic Types of property are IConvertibles before casting them to IConvertible
                var typeArguments = t.IsGenericType ? t.GetGenericArguments() : t.GetInterfaces()
                    .FirstOrDefault(face => face.IsGenericType && face.GetGenericTypeDefinition() == typeof(IEnumerable<>))?
                    .GetGenericArguments();
                if (typeArguments == null || typeArguments.Length != 1 || !typeof(IConvertible).IsAssignableFrom(typeArguments[0])) {
                    Logger.LogWarning("{Card} failed to serialize IEnumerable<> {Property} because its generic arguments were improper (multiple, or not IConvertible).", this, propInfo.Name);
                    convertibles = [];
                    return false;
                }

                convertibles = [..propValue.Cast<IConvertible>()];
                return true;

            case Type t when t.IsEnum:
                if (propInfo.GetValue(this) is not Enum enumValue) {
                    Logger.LogWarning("{Card} failed to serialize Enum {Property} because it returned a null value.", this, propInfo.Name);
                    convertibles = [];
                    return false;
                }
                if (Enum.GetUnderlyingType(enumValue.GetType()) != typeof(int)) {
                    Logger.LogWarning("{Card} attempted to serialize Enum {Property}, but its underlying type was not int.", this, propInfo.Name);
                    convertibles = [];
                    return false;
                }
                convertibles = [Convert.ToInt32(enumValue)];
                return true;

            case Type t when t.IsPrimitive:
                if (propInfo.GetValue(this) is not IConvertible convertible) {
                    Logger.LogWarning("{Card} failed to serialize convertible/primitive {Property} because it returned a null value.", this, propInfo.Name);
                    convertibles = [];
                    return false;
                }
                convertibles = [convertible];
                return true;

            default:
                Logger.LogWarning("{Card} failed to serialize convertible/primitive {Property} because it does not implement IConvertible (it is not a string, Enum, or primitive type).", this, propInfo.Name);
                convertibles = [];
                return false;
        }
    }

    bool IBinarySerializable.LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try {
            var cardProps = this.GetType().GetProperties();
            int loadedNumProperties = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
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

            int propIndex = -1;
            while (propIndex < numProperties) {
                propIndex++;
                string propName = cardProps[propIndex].Name;
                if (propName == nameof(CardSet) || propName == nameof(PropertySerializableTypeMap) || propName == nameof(TypeName) || propName == nameof(Logger))
                    continue;

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
                if (propType.IsAssignableFrom(typeof(IEnumerable)) == false) {
                    if (numValsLoaded > 1) {
                        Logger.LogError("{Card} attempted to load from binary, but there was a property type mismatch: the property was not an IEnumerable, but it attempted to load multiple values.", this);
                        return false;
                    }
                    cardProps[propIndex].SetValue(this, BinarySerializer.ReadConvertible(reader, serialType));
                }
                else
                    cardProps[propIndex].SetValue(this, BinarySerializer.ReadConvertibles(reader, serialType, numValsLoaded)[0]);
            }
        }
        catch (Exception ex) {
            Logger.LogError("An exception was thrown while loading {Card}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
}
