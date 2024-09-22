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

    (Type SerialType, IConvertible[] SerialValues)[] IBinarySerializable.GetBinarySerials()
    {
        Type instanceType = this.GetType();
        PropertyInfo[] instanceProperties = instanceType.GetProperties();

        List<(Type SerialType, IConvertible[] SerialValues)> serialData = [];
        serialData.Add((typeof(int), [instanceProperties.Length - 2])); // CardSet and SerialPropertyTypeMap are excluded since they are initialized independently.
        foreach (PropertyInfo propInfo in instanceProperties) {
            string propName = propInfo.Name;
            if (propName == nameof(CardSet) || propName == nameof(PropertySerializableTypeMap))
                continue;
            if (PropertySerializableTypeMap[propName] is not Type mappedType) {
                Logger.LogWarning("{Card} binary serialization failed on {Property} because a corresponding Type was not found in {Map}.", this, propName, PropertySerializableTypeMap);
                continue;
            }

            serialData.Add((typeof(string), [propName])); // Name is used by SerialPropertyTypeMap

            if (!TryGetConvertibles(propInfo, out IConvertible[] propConvertibles) || propConvertibles == null) {
                Logger.LogWarning("{Card} binary serialization failed on {Property}.", this, propName);
                continue;
            }

            serialData.Add((typeof(int), [propConvertibles.Length]));
            foreach (var datum in propConvertibles)
                serialData.Add((mappedType, [datum]));
        }


        serialData.Insert(0, (typeof(int), [serialData.Count])); // since this default method dynamically builds the serials, total length isn't available until here

        return [.. serialData];
    }
    bool TryGetConvertibles(PropertyInfo propInfo, out IConvertible[] convertibles)
    {
        switch (propInfo.PropertyType) {
            case Type t when typeof(IEnumerable).IsAssignableFrom(t):
                var propValue = propInfo.GetValue(this) as IEnumerable;
                if (propValue == null) {
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
        var cardProps = this.GetType().GetProperties();
        int loadedNumProperties = reader.ReadInt32();
        int numProperties = cardProps.Length;
        if (numProperties - 2 != loadedNumProperties) { // recall that CardSet and PropertySerializableTypeMap are excluded
            Logger.LogError("{Card} attempted to load from binary, but there was a property count mismatch.", this);
            return false;
        }

        int propIndex = -1;
        while (propIndex < numProperties) {
            propIndex++;
            string propName = cardProps[propIndex].Name;
            if (propName == nameof(CardSet) || propName == nameof(PropertySerializableTypeMap))
                continue;

            string loadedName = reader.ReadString();
            if (propName != loadedName) {
                Logger.LogError("{Card} attempted to load from binary, but there was a property name mismatch.", this);
                return false;
            }

            int numValsLoaded = reader.ReadInt32();
            Type propType = cardProps[propIndex].PropertyType;
            if (propType.IsAssignableFrom(typeof(IEnumerable)) == false && numValsLoaded > 1) {
                Logger.LogError("{Card} attempted to load from binary, but there was a property type mismatch: the property was not an IEnumerable, but it attempted to load multiple values.", this);
                return false;
            }

            if (PropertySerializableTypeMap[loadedName] is not Type serialType) {
                Logger.LogError("{Card} attempted to load from binary, but the name of a loaded property was not found in {Map}.", this, PropertySerializableTypeMap);
                return false;
            }

            BinarySerializer.ReadConvertible(reader,,)
            for (int i = 0; i < numValsLoaded; i++)
                
        }

    }
}
