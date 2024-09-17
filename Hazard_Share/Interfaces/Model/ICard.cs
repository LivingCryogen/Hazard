using Hazard_Share.Enums;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Reflection;

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

    IConvertible?[] IBinarySerializable.GetSaveData()
    {
        Type instanceType = this.GetType();
        PropertyInfo[] instanceProperties = instanceType.GetProperties();

        List<string> propertyNames = [];
        List<Type> serialTypes = [];
        List<object?[]?> propertyValues = [];

        foreach (PropertyInfo propInfo in instanceProperties) {
            var propName = propInfo.Name;
            if (propName != nameof(PropertySerializableTypeMap) && propName != nameof(CardSet)) {
                propertyNames.Add(propName);
                if (TryGetPropertySerials(propInfo, out IConvertible?[] serialValues)) {
                    if (serialType == null)
                        throw new NullReferenceException(nameof(serialType));
                    serialTypes.Add(serialType);
                    propertyValues.Add(propValues);
                }
                else throw new ArgumentException($"{nameof(TryGetPropertySerials)} failed on {propName} of {this}.");
            }
        }

        var propertySerials = propertyValues.ToArray();

        return (TypeName: instanceType.Name, PropertyNames: propertyNames.ToArray(), SerialTypes: serialTypes.ToArray(), PropertySerials: propertyValues.ToArray());
    }
    bool TryGetPropertySerials(PropertyInfo propInfo, out IConvertible?[]? serialValues)
    {
        if (typeof(IEnumerable).IsAssignableFrom(propInfo.PropertyType)) { // 'true' if the property implements IEnumerable (is a collection)

        }

        serialValues = propInfo.PropertyType switch {
            Type t when typeof(IEnumerable).IsAssignableFrom(t) => GetEnumerableConvertibles(propInfo),
            Type t when t.IsEnum => [(int?)propInfo.GetValue(this)],
            Type t when t.IsPrimitive => [(IConvertible?)propInfo.GetValue(this)],
            _ => null
        }; 

        return serialValues != null;
    }

    private IConvertible?[]? GetEnumerableConvertibles(PropertyInfo propInfo)
    {
        try {
            return ((IEnumerable?)propInfo.GetValue(this))?.Cast<IConvertible?>().ToArray();
        } catch (InvalidCastException invalidCast) {
            Logger.LogError("{ICard} attempted to convert {Property} for binary serialization, but ", this, propInfo.Name, );
            throw;
        }
    }
    /// <summary>
    /// A helper method that attempts to convert property values to serialized objects.
    /// </summary>
    /// <param name="propInfo">The <see cref="PropertyInfo"/> of the property to be serialized. Usually obtained via reflection.<br/><example>E.g.:<code>var propInfoList = this.GetType().GetProperties();</code></example></param>
    /// <param name="serialType">The <see cref="Type"/> that the value of the property should be converted to for serialization.</param>
    /// <param name="serials">The converted values as an array of nullable objects.</param>
    /// <param name="logger">The <see cref="ILogger"/> provided by DI.</param>
    /// <returns><see langword="true"/> if the conversion to serializable <see cref="Type"/>s was succesful; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if an <see cref="IEnumerable"/> property contains an object which fails <see cref="TryConvertToSerial"/>.</exception>
    //bool TryGetPropertySerials(PropertyInfo propInfo, out Type? serialType, out object?[]? serials, ILogger logger)
    //{
    //    if (propInfo == null) {
    //        serials = [];
    //        serialType = null;
    //        return false;
    //    }
    //    var propName = propInfo.Name;
    //    var propType = propInfo.PropertyType;

    //    if (propType.IsPrimitive) {
    //        serials = [propInfo.GetValue(this, null)!];
    //        serialType = propType;
    //        return true;
    //    }

    //    if (PropertySerializableTypeMap == null) {
    //        serials = [];
    //        serialType = null;
    //        logger.LogWarning("ICard {Card} attempted to serialize its properties with a null PropertySerializableTypeMap.", this);
    //        return false;
    //    }
    //    Type? targetType = PropertySerializableTypeMap[propName];

    //    bool propEnumerable = typeof(IEnumerable).IsAssignableFrom(propType) && propType != typeof(string); // checks whether the property type implements IEnumerable (is a collection), and is not a string.
    //    if (!propEnumerable) {
    //        serialType = targetType;
    //        if (TryConvertToSerial(propInfo.GetValue(this, null), targetType, out object? serial, logger)) {
    //            serials = [serial];
    //            return true;
    //        }

    //        serials = null;
    //        return false;
    //    }

    //    var collection = propInfo.GetValue(this, null);
    //    if (collection == null) {
    //        if (Nullable.GetUnderlyingType(targetType) == null) {
    //            serials = null;
    //            serialType = null;
    //            return false;
    //        }

    //        serials = null;
    //        serialType = targetType;
    //        return true;
    //    }

    //    var enumerator = ((IEnumerable)collection).GetEnumerator();
    //    List<object?> convertedValues = [];
    //    while (enumerator.MoveNext()) { // returns false if it passes the end of the collection, and begins *before* the first element
    //        if (TryConvertToSerial(enumerator.Current, targetType, out object? convertedValue, logger))
    //            convertedValues.Add(convertedValue);
    //        else {
    //            logger.LogWarning("ICard {card} failed to convert {Property} to {Type}. A null value was returned instead.", this, enumerator.Current, targetType);
    //            convertedValues.Add(null);
    //        }
    //    }

    //    serials = [.. convertedValues];
    //    serialType = targetType;
    //    return true;
    //}
    ///// <summary>
    ///// Converts an <see cref="ICard"/> property to a serializable (primitive) <see cref="Type"/>.
    ///// </summary>
    ///// <remarks>
    ///// Non-primitive property types require registry in <see cref="PropertySerializableTypeMap"/> and may require extension here.
    ///// </remarks>
    ///// <param name="toConvert">The property value to convert.</param>
    ///// <param name="serialType">The target conversion <see cref="Type"/> for serialization.</param>
    ///// <param name="converted">The converted value.</param>
    ///// <param name="logger">The <see cref="ILogger"/> provided by DI.</param>
    ///// <returns><see langword="true"/> if conversion is successful; otherwise, <see langword="false"/>.</returns>
    //bool TryConvertToSerial(object? toConvert, Type serialType, out object? converted, ILogger logger)
    //{
    //    if (toConvert == null) {
    //        if (Nullable.GetUnderlyingType(serialType) == null) {
    //            converted = null;
    //            return false;
    //        }
    //        converted = null;
    //        return true;
    //    }

    //    if (toConvert is IConvertible) {
    //        try {
    //            converted = Convert.ChangeType(toConvert, serialType);
    //            return true;
    //        } catch (InvalidCastException e) {
    //            logger?.LogError("ICard {Card} threw a casting exception when trying Convert.ChangeType({PropertyType}, {SerialType}): {Message}, {Source}.", this, toConvert, serialType, e.Message, e.Source);
    //            converted = null;
    //            return false;
    //        }
    //    }

    //    Type currentType = toConvert.GetType();

    //    if (serialType == typeof(int)) {
    //        if (toConvert == null) {
    //            converted = null;
    //            return false;
    //        }
    //        else {
    //            if (toConvert is int intConvert) {
    //                converted = intConvert;
    //                return true;
    //            }
    //            else {
    //                if (toConvert is Enum) {
    //                    if (Enum.IsDefined(currentType, toConvert)) {
    //                        converted = Enum.Parse(currentType, toConvert.ToString()!);
    //                        return true;
    //                    }
    //                    else {
    //                        converted = null;
    //                        return false;
    //                    }
    //                }

    //                if (int.TryParse(toConvert.ToString(), out int result)) {
    //                    converted = result;
    //                    return true;
    //                }
    //                else {
    //                    converted = null;
    //                    return false;
    //                }

    //                // Additional conversions to Int can be added here by Implementers
    //            }
    //        }
    //    }

    //    if (serialType == typeof(string)) {
    //        if (toConvert == null) {
    //            converted = null;
    //            return true;
    //        }

    //        // Explicit conversions to string can be added by Implementers here

    //        try {
    //            converted = (string)toConvert;
    //        } catch (Exception e) {
    //            logger.LogDebug("ICard {Card} attempted to cast {Value} as a string, but an exception was thrown: {Message}; {Source};", this, toConvert, e.Message, e.Source);
    //        }

    //        try {
    //            converted = toConvert.ToString();
    //        } catch (Exception e) {
    //            logger.LogDebug("ICard {Card} attempted to convert {Value} with a .ToString() call, but an exception was thrown: {Message}; {Source};", this, toConvert, e.Message, e.Source);
    //            converted = null;
    //        }

    //        if (converted == null)
    //            return false;
    //        else
    //            return true;
    //    }

    //    if (serialType == typeof(bool)) {
    //        if (toConvert == null) {
    //            converted = null;
    //            return false;
    //        }

    //        if (toConvert is bool boolConvert) {
    //            converted = boolConvert;
    //            return true;
    //        }

    //        if (toConvert is int intConvert) {
    //            if (intConvert == 0) {
    //                converted = false;
    //                return true;
    //            }
    //            if (intConvert == 1) {
    //                converted = true;
    //                return true;
    //            }

    //            // Additional Int -> Bool conversions can be added by Implementers here
    //        }

    //        if (toConvert is string strConvert) {
    //            var stringToConvert = strConvert;
    //            if (string.Equals(stringToConvert, "false", StringComparison.OrdinalIgnoreCase)) {
    //                converted = false;
    //                return true;
    //            }

    //            if (string.Equals(stringToConvert, "true", StringComparison.OrdinalIgnoreCase)) {
    //                converted = true;
    //                return true;
    //            }

    //            // Additional String -> Bool conversions can be added by Implementers here

    //            converted = null;
    //            return false;
    //        }

    //        // Additional Boolean conversions can be added by Implementers here
    //    }

    //    // Conversions for other serializable Types can be added by Implementers here

    //    converted = null;
    //    return false;
    //}
    ///// <summary>
    ///// Loads property values of this <see cref="ICard"/> from binary.
    ///// </summary>
    ///// <remarks>
    ///// Implementing this method is necessary for <see cref="Hazard_Model.DataAccess.BinarySerializer.LoadCardList"/> to properly handle the <see cref="ICard"/>.
    ///// </remarks>
    ///// <param name="reader">The <see cref="BinaryReader"/> from <see cref="Hazard_Model.DataAccess.BinarySerializer.LoadCardList"/>.</param>   
    ///// <param name="propName">The name of the property to which the next value(s) from <paramref name="reader"/> belongs.</param>
    ///// <param name="numValues">The number of values the property is receiving.</param>
    ///// <returns><see langword="true"/> if the value is read and the property initialized with that value; otherwise, <see langword="false"/>.</returns>
    //bool InitializePropertyFromBinary(BinaryReader reader, string propName, int numValues);
}
