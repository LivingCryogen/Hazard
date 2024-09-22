using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hazard_Share.Interfaces.Model;

public interface IBinarySerializable
{
    // bool LoadFromSerials((Type SerialType, IConvertible[] SerialValues)[] serials);
    (Type SerialType, IConvertible[] SerialValues)[] GetBinarySerials();
}
