using Shared.Interfaces.Model;

namespace Shared.Services.Serializer;
/// <summary>
/// A collection of serialized data and metadata gathered from an <see cref="IBinarySerializable"/>.
/// </summary>
/// <remarks>
/// Used by *reflection-dependent* paths of <see cref="BinarySerializer"/> via <see cref="IBinarySerializable.GetBinarySerials"/>.<br/>
/// If you are implementing custom serialization (rather than using default interface methods), 
/// consider using the generic <see cref="BinarySerializer"/> methods directly instead of this struct.
/// </remarks>
public readonly struct SerializedData
{
    /// <summary>
    /// Constructs SerializedData for a single, untagged value.
    /// </summary>
    /// <param name="serialType">The type of the value in <paramref name="value"/>.</param>
    /// <param name="value">The serializable value.</param>
    public SerializedData(Type serialType, IConvertible value)
    {
        SerialType = serialType;
        SerialValues = [value];
    }
    /// <summary>
    /// Constructs SerializedData for a single, tagged value.
    /// </summary>
    /// <param name="serialType">The type of the value in <paramref name="value"/>.</param>
    /// <param name="value">The serializable value.</param>
    /// <param name="tag">A tag (will precede the type and value when written via <see cref="BinarySerializer"/>).</param>
    public SerializedData(Type serialType, IConvertible value, string tag)
    {
        SerialType = serialType;
        SerialValues = [value];
        Tag = tag;
    }
    /// <summary>
    /// Constructs SerializedData for a set of <see cref="IConvertible"/>values with a tag when they may be part of an IConvertible collection.
    /// </summary>
    /// <param name="serialType">The type of the values which are now stored in <paramref name="serialValues"/>.</param>
    /// <param name="serialValues"><see cref="IConvertible"/>s storing (potentially converted) values for type <paramref name="serialType"/>.</param>
    /// <param name="tag">A metadata tag for the values.</param>
    public SerializedData(Type serialType, IConvertible[] serialValues, string tag)
    {
        SerialType = serialType;
        if (BinarySerializer.IsIConvertibleCollection(serialType, out Type? memberType))
            MemberType = memberType;
        SerialValues = serialValues;
        Tag = tag;
    }
    /// <summary>
    /// Constructs SerializedData for a set of <see cref="IConvertible"/> values when they may be part of an IConvertible collection.
    /// </summary>
    /// <inheritdoc cref="SerializedData.SerializedData(Type, IConvertible[], string?)"/>
    public SerializedData(Type serialType, IConvertible[] serialValues)
    {
        SerialType = serialType;
        if (BinarySerializer.IsIConvertibleCollection(serialType, out Type? memberType))
            MemberType = memberType;
        SerialValues = serialValues;
    }
    /// <summary>
    /// Gets or inits the Type of the members of <see cref="SerialType"/>, if any.
    /// </summary>
    /// <value>
    /// If <see cref="SerialType"/> is a serializable collection type, the type of its members; otherwise, <see langword="null"/>.
    /// </value>
    public Type? MemberType { get; init; } = null;
    /// <summary>
    /// Gets or inits the type associated with the <see cref="SerialValues"/>.
    /// </summary>
    /// <value>
    /// To be compatible with <see cref="BinarySerializer"/>, this should be a type of <see cref="byte"/> or <see cref="IConvertible"/>, or an <see cref="System.Collections.IEnumerable"/> of them.<br/>
    /// </value>
    public Type SerialType { get; init; }
    /// <summary>
    /// Gets or inits the values to be serialized. 
    /// </summary>
    /// <value>
    /// An array of <see cref="IConvertible"/> values provided by an <see cref="IBinarySerializable"/> object and cast (or converted) to <see cref="IConvertible"/>.
    /// </value>
    /// <remarks>
    /// Will be encoded and written by <see cref="BinarySerializer"/>.
    /// </remarks>
    public IConvertible[] SerialValues { get; init; }
    /// <summary>
    /// Gets or inits a metadata tag.
    /// </summary>
    /// <value>
    /// A tag to precede <see cref="SerialValues"/> when written to a file with <see cref="BinarySerializer"/>; otherwise, <see langword="null"/>.
    /// </value>
    /// <remarks>
    /// To properly read tagged data with <see cref="BinarySerializer"/>, <see cref="BinaryReader.ReadString()"/> must be called before the requisite <see cref="BinarySerializer.ReadConvertible"/><br/>
    /// or <see cref="BinarySerializer.ReadConvertibles"/>. <see cref="BinarySerializer.WriteTaggedConvertible(BinaryWriter, Type, IConvertible, string)"/>.
    /// </remarks>
    public string? Tag { get; init; } = null;
}
