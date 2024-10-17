using Share.Interfaces.Model;

namespace Share.Services.Serializer;
/// <summary>
/// A collection of serialized data and metadata gathered from an <see cref="IBinarySerializable"/>.
/// </summary>
/// <remarks>
/// Used by <see cref="BinarySerializer"/> via <see cref="IBinarySerializable.GetBinarySerials"/>. 
/// </remarks>
public readonly struct SerializedData
{
    /// <summary>
    /// Constructs a <see cref="SerializedData"/> for a set of <see cref="IConvertible"/>values with a <see cref="string">tag</see>.
    /// </summary>
    /// <param name="serialType">The <see cref="Type"/> of the values which are now stored in <paramref name="serialValues"/>.</param>
    /// <param name="serialValues">An array of <see cref="IConvertible"/> storing (potentially converted) values for type <paramref name="serialType"/>.</param>
    /// <param name="tag">A metadata <see cref="string"></see>tag for the values.</param>
    public SerializedData(Type serialType, IConvertible[] serialValues, string? tag)
    {
        SerialType = serialType;
        SerialValues = serialValues;
        Tag = tag;
    }
    /// <summary>
    /// Constructs a <see cref="SerializedData"/> for a set of <see cref="IConvertible"/> values.
    /// </summary>
    /// <inheritdoc cref="SerializedData.SerializedData(Type, IConvertible[], string?)"/>
    public SerializedData(Type serialType, IConvertible[] serialValues)
    {
        SerialType = serialType;
        SerialValues = serialValues;
    }
    /// <summary>
    /// Gets or inits the serial <see cref="Type"/> associated with the <see cref="SerialValues"/>.
    /// </summary>
    /// <value>
    /// A <see cref="Type"/>. To be compatible with <see cref="BinarySerializer"/>, this should be a type of <see cref="byte"/> or implementer of <see cref="IConvertible"/>.<br/>
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
    /// Gets or inits a metadata tag for this <see cref="SerializedData"/>.
    /// </summary>
    /// <value>
    /// A <see cref="string"/>, if this <see cref="SerializedData"/>'s <see cref="SerialValues"/> are to be preceded by one when written to <see cref="BinaryReader.BaseStream"/>; otherwise, <see langword="null"/>.
    /// </value>
    /// <remarks>
    /// To properly read tagged data with <see cref="BinarySerializer"/>, <see cref="BinaryReader.ReadString()"/> must be called before the requisite <see cref="BinarySerializer.ReadConvertible"/><br/>
    /// or <see cref="BinarySerializer.ReadConvertibles"/>. <see cref="BinarySerializer.WriteTaggedConvertible(BinaryWriter, Type, IConvertible, string)"/>.
    /// </remarks>
    public string? Tag { get; init; } = null;
}
