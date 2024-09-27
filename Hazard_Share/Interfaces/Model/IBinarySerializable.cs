using Hazard_Share.Services.Serializer;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hazard_Share.Interfaces.Model;

public interface IBinarySerializable
{
    bool LoadFromBinary(BinaryReader reader);
    Task<SerializedData[]> GetBinarySerials();
}
