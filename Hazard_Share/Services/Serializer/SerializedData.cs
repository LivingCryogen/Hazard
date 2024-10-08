namespace Hazard_Share.Services.Serializer;

public readonly struct SerializedData
{
    public SerializedData(Type serialType, IConvertible[] serialValues, string? tag)
    {
        SerialType = serialType;
        SerialValues = serialValues;
        Tag = tag;
    }
    public SerializedData(Type serialType, IConvertible[] serialValues)
    {
        SerialType = serialType;
        SerialValues = serialValues;
    }
    public Type SerialType { get; init; }
    public IConvertible[] SerialValues { get; init; }
    public string? Tag { get; init; } = null;
}
