using System;
using System.Collections.Generic;
using System.Text;

namespace Hazard_Share.Services.Serializer;

public readonly struct SerializedData(Type serialType, IConvertible[] serialValues, bool writeTypeName)
{
    public Type SerialType { get; init; } = serialType;
    public IConvertible[] SerialValues { get; init; } = serialValues;
    public bool WriteTypeName { get; init; } = writeTypeName;
}
