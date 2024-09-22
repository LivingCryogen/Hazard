using Hazard_Share.Interfaces.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hazard_Share.Services.Serializer;

public static class BinarySerializer
{
    private static byte[] ConvertibleToBytes(Type type, IConvertible value)
    {
        if (type == typeof(string))
            return Encoding.UTF8.GetBytes((string)value);
        else if (type.IsEnum)
            return BitConverter.GetBytes(Convert.ToInt64(value));
        else
            return BitConverter.GetBytes(Convert.ToDouble(value));
    }
    private static IConvertible BytesToConvertible(Type type, byte[] bytes)
    {
        if (type == typeof(string))
            return Encoding.UTF8.GetString(bytes);
        if (type.IsEnum)
            return (IConvertible)Enum.ToObject(type, BitConverter.ToInt64(bytes, 0));

        double doubleVal = BitConverter.ToDouble(bytes, 0);
        return (IConvertible)Convert.ChangeType(doubleVal, type);
    }
    private static void WriteTypelessConvertible(BinaryWriter writer, Type type, IConvertible value)
    {
        byte[] bytes = ConvertibleToBytes(type, value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }
    private static void WriteTypelessConvertibles(BinaryWriter writer, Type type, IConvertible[] values)
    {
        var arrayPool = System.Buffers.ArrayPool<byte>.Shared;
        foreach (IConvertible value in values) {
            byte[] bytes = ConvertibleToBytes(type, value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
            arrayPool.Return(bytes, clearArray: true);
        }
    }
    private static IConvertible ReadTypelessConvertible(BinaryReader reader, Type type)
    {
        int length = reader.ReadInt32();
        byte[] bytes = reader.ReadBytes(length);
        return BytesToConvertible(type, bytes);
    }
    private static IConvertible[] ReadTypelessConvertibles(BinaryReader reader, Type type, int numValues)
    {
        List<IConvertible> readConvertibles = [];
        for(int i = 0; i < numValues; i++)
            readConvertibles.Add(ReadTypelessConvertible(reader,type));
        return [.. readConvertibles];
    }

    private static void WriteTypedConvertible(BinaryWriter writer, IConvertible value)
    {
        Type valueType = value.GetType();
        if (valueType.AssemblyQualifiedName is not string typeName)
            throw new ArgumentException($"Reflection failed to find a qualified type name for {value}.");
        writer.Write(typeName);  
        WriteTypelessConvertible(writer, valueType, value);
    }
    private static void WriteTypedConvertible(BinaryWriter writer, Type type, IConvertible value)
    {
        if (type.AssemblyQualifiedName is not string typeName)
            throw new ArgumentException($"Failed to find a qualified type name for {type}.");
        writer.Write(typeName);
        WriteTypelessConvertible(writer, type, value);
    }
    private static void WriteTypedConvertible(BinaryWriter writer, Type type, string qualifiedTypeName, IConvertible value)
    {
        writer.Write(qualifiedTypeName);
        WriteTypelessConvertible(writer, type, value);
    }
    private static void WriteTypedConvertibles(BinaryWriter writer, IConvertible[] values)
    {
        foreach (IConvertible value in values)
            WriteTypedConvertible(writer, value);
    }
    private static void WriteTypedConvertibles(BinaryWriter writer, Type type, IConvertible[] values)
    {
        if (type.AssemblyQualifiedName is not string typeName)
            throw new ArgumentException($"Failed to find a qualified type name for {type}.");

        foreach (IConvertible value in values)
            WriteTypedConvertible(writer, type, typeName, value);
    }
    private static IConvertible ReadTypedConvertible(BinaryReader reader)
    {
        string typeName = reader.ReadString();
        if (Type.GetType(typeName) is not Type type)
            throw new ArgumentException($"Failed to find a type with name {typeName}.");
        return ReadTypelessConvertible(reader, type);
    }
    private static IConvertible[] ReadTypedConvertibles(BinaryReader reader, Type type, int numValues)
    {

    }

    public static bool Save(IBinarySerializable serializableObject, BinaryWriter writer)
    {
        var saveData = serializableObject.get
    }
    public static bool Load(IBinarySerializable serializableObject, BinaryReader reader)
    {
        
    }
}
