using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hazard_Share.Interfaces.Model;

public interface IBinarySerializable
{
    IEnumerable<IConvertible> GetSaveData();
    bool LoadSaveData(BinaryReader reader);
}
