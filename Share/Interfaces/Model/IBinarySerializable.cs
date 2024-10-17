using Share.Services.Serializer;

namespace Share.Interfaces.Model;
/// <summary>
/// Stipulates an object is serializable using <see cref="BinarySerializer"/>.
/// </summary>
public interface IBinarySerializable
{
    /// <summary>
    /// Loads the <see cref="IBinarySerializable"/> with binary values read from a file.
    /// </summary>
    /// <param name="reader">A <see cref="BinaryReader"/> whose <see cref="BinaryReader.BaseStream"/> was previously written to using <see cref="BinarySerializer"/> and <see cref="GetBinarySerials"/>.</param>
    /// <returns><see langword="true"/> if the load succeeded without exceptions; otherwise, <see langword="false"/>.</returns>
    bool LoadFromBinary(BinaryReader reader);
    /// <summary>
    /// Asynchronously serializes the <see cref="IBinarySerializable"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> whose result is an array of <see cref="SerializedData"/>.</returns>
    Task<SerializedData[]> GetBinarySerials();
}
