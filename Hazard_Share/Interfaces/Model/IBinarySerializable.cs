using Hazard_Share.Services.Serializer;

namespace Hazard_Share.Interfaces.Model;

public interface IBinarySerializable
{
    bool LoadFromBinary(BinaryReader reader);
    Task<SerializedData[]> GetBinarySerials();
}
