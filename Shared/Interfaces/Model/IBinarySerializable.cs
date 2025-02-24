using Shared.Services.Serializer;
using System.IO;

namespace Shared.Interfaces.Model;
/// <summary>
/// Stipulates an object is serializable using <see cref="BinarySerializer"/>.
/// </summary>
public interface IBinarySerializable
{
    /// <summary>
    /// Loads the <see cref="IBinarySerializable"/> with binary values read from a file.
    /// </summary>
    /// <param name="reader">A reader whose <see cref="BinaryReader.BaseStream"/> was previously written to using <see cref="BinarySerializer"/> and <see cref="GetBinarySerials"/>.</param>
    /// <returns><see langword="true"/> if the load succeeded without exceptions; otherwise, <see langword="false"/>.</returns>
    bool LoadFromBinary(BinaryReader reader);
    /// <summary>
    /// Asynchronously serializes the <see cref="IBinarySerializable"/>.
    /// </summary>
    /// <returns>A task whose result contains serialized object data.</returns>
    Task<SerializedData[]> GetBinarySerials();
}
