using System;
using System.Collections.Generic;
using System.Text;

namespace Hazard_Share.Services.Serializer;

public readonly struct SerializedData(Type serialType, IConvertible[] serialValues, string? tag)
{
    public Type SerialType { get; init; } = serialType;
    public IConvertible[] SerialValues { get; init; } = serialValues;
    public string? Tag { get; init; } = tag;
}
