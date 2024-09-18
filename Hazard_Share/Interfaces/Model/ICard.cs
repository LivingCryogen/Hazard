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

    (Type SerialType, IConvertible?[] SerialValues)[] IBinarySerializable.GetSaveData()
    {
        Type instanceType = this.GetType();
        PropertyInfo[] instanceProperties = instanceType.GetProperties();
        List<(Type, IConvertible?[])> saveData = [(typeof(int), [instanceProperties.Length - 1])]; // CardSet is excluded since it is initialized independently.
        foreach (PropertyInfo propInfo in instanceProperties) {
            if (propInfo.Name == nameof(CardSet))
                continue;
            if (!TryGetPropertySerials(propInfo, out IConvertible?[]? serialValues) || serialValues == null) {
                Logger.LogWarning("{Card} binary serialization failed on {Property}.", this, propInfo.Name);
                continue;
            }
            /// BRING BACK PROPERTY SERIALIZABLE TYPE MAP!! <----
            foreach(var serialValue in serialValues)
            saveData.Add((typeof(int), [serialValues.Length])); // To read back data, a preceding length is required for each property
            saveData.AddRange(serialValues);
        }

        return flatPropData.ToArray();
    }
    bool TryGetPropertySerials(PropertyInfo propInfo, out IConvertible?[]? serialValues)
    {
        serialValues = propInfo.PropertyType switch {
            Type t when typeof(IEnumerable).IsAssignableFrom(t) => GetEnumerableConvertibles(propInfo),
            Type t when typeof(IBinarySerializable).IsAssignableFrom(t) => ((IBinarySerializable?)propInfo.GetValue(this))?.GetSaveData(),
            Type t when t.IsEnum => [(int?)propInfo.GetValue(this)],
            Type t when t.IsPrimitive => [(IConvertible?)propInfo.GetValue(this)],
            _ => null
        }; 

        return serialValues != null;
    }
    private IConvertible?[]? GetEnumerableConvertibles(PropertyInfo propInfo)
    {
        var propValue = (IEnumerable?)propInfo.GetValue(this);

        List<IConvertible?> convertedMembers = [];
        IEnumerator? memberEnumerator = propValue?.GetEnumerator();
        while (memberEnumerator?.MoveNext() ?? false) {
            var memberType = memberEnumerator.Current.GetType();
            if (memberType.IsEnum)
                convertedMembers.Add((int)memberEnumerator.Current);
            else if (memberType is IConvertible)
                convertedMembers.Add((IConvertible)memberEnumerator.Current);
            else {
                Logger.LogError("{ICard} attempted to convert the collection {Property} for binary serialization, but its member {Current} was not an Enum or an IConvertible (primitive).", this, propInfo.Name, memberEnumerator.Current);
                return null;
            }
        }

        return convertedMembers.ToArray();
    }
    bool IBinarySerializable.LoadSaveData(BinaryReader reader)
    {
        var cardProps = this.GetType().GetProperties();
        int numLoadProperties = reader.ReadInt32();
        if (numLoadProperties != cardProps.Length - 1) {
            Logger.LogError("{Card} attempted to load save data, but the {Number} of properties stored did not match its properties", this, numLoadProperties);
            return false;
        }

        foreach()

    }
}
