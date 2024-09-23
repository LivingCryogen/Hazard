using Hazard_Share.Interfaces.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hazard_Share.Services.Serializer;

public static class BinarySerializer
{
    #region Encoding Methods
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
    #endregion
    #region Serialization Methods

    private static void WriteConvertible(BinaryWriter writer, Type type, IConvertible value)
    {
        byte[] bytes = ConvertibleToBytes(type, value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }
    private static void WriteConvertibles(BinaryWriter writer, Type type, IConvertible[] values)
    {
        var arrayPool = System.Buffers.ArrayPool<byte>.Shared;
        foreach (IConvertible value in values) {
            byte[] bytes = ConvertibleToBytes(type, value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
            arrayPool.Return(bytes, clearArray: true);
        }
    }
    public static IConvertible ReadConvertible(BinaryReader reader, Type type)
    {
        int length = reader.ReadInt32();
        byte[] bytes = reader.ReadBytes(length);
        return BytesToConvertible(type, bytes);
    }
    public static IConvertible[] ReadConvertibles(BinaryReader reader, Type type, int numValues)
    {
        List<IConvertible> readConvertibles = [];
        for(int i = 0; i < numValues; i++)
            readConvertibles.Add(ReadConvertible(reader,type));
        return [.. readConvertibles];
    }

    //private static void WriteConvertibleWithType(BinaryWriter writer, IConvertible value)
    //{
    //    Type valueType = value.GetType();
    //    if (valueType.AssemblyQualifiedName is not string typeName)
    //        throw new ArgumentException($"Reflection failed to find a qualified type name for {value}.");
    //    writer.Write(typeName);  
    //    WriteConvertible(writer, valueType, value);
    //}
    private static void WriteConvertibleWithType(BinaryWriter writer, Type type, IConvertible value)
    {
        if (type.AssemblyQualifiedName is not string typeName)
            throw new ArgumentException($"Failed to find a qualified type name for {type}.");
        writer.Write(typeName);
        WriteConvertible(writer, type, value);
    }
    private static void WriteConvertibleWithType(BinaryWriter writer, Type type, string typeName, IConvertible value)
    {
        writer.Write(typeName);
        WriteConvertible(writer, type, value);
    }
    //private static void WriteTypedConvertibles(BinaryWriter writer, IConvertible[] values)
    //{
    //    foreach (IConvertible value in values)
    //        WriteTypedConvertible(writer, value);
    //}
    private static void WriteConvertiblesWithType(BinaryWriter writer, Type type, IConvertible[] values)
    {
        if (type.AssemblyQualifiedName is not string typeName)
            throw new ArgumentException($"Failed to find a qualified type name for {type}.");

        foreach (IConvertible value in values)
            WriteConvertibleWithType(writer, type, typeName, value);
    }
    //private static IConvertible ReadTypedConvertible(BinaryReader reader)
    //{
    //    string typeName = reader.ReadString();
    //    if (Type.GetType(typeName) is not Type type)
    //        throw new ArgumentException($"Failed to find a type with name {typeName}.");
    //    return ReadTypelessConvertible(reader, type);
    //}
    //private static IConvertible[] ReadTypedConvertibles(BinaryReader reader, int numValues)
    //{
    //    List<IConvertible> readConvertibles = [];
    //    for (int i = 0; i < numValues; i++)
    //        readConvertibles.Add(ReadTypedConvertible(reader));
    //    return [.. readConvertibles];
    //}
    #endregion

    public static bool Save(IBinarySerializable[] serializableObjects, string fileName, bool newFile, ILogger logger)
    {
        if (newFile) {
            using FileStream fileStream = new(fileName, FileMode.Create, FileAccess.Write);
            using BinaryWriter writer = new(fileStream);

            bool errors = false;
            foreach (var obj in serializableObjects)
                if (!WriteSerializableObject(obj, writer, logger))
                    errors = true;

            return !errors;
        }
        else {
            using FileStream fileStream = new(fileName, FileMode.Truncate, FileAccess.Write);
            using BinaryWriter writer = new(fileStream);

            bool errors = false;
            foreach (var obj in serializableObjects)
                if (!WriteSerializableObject(obj, writer, logger))
                    errors = true;

            return !errors;
        }
    }
    public static bool Load(IBinarySerializable[] serializableObjects, string fileName, ILogger logger)
    {
        using FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(fileStream);

        bool errors = false;
        foreach (var obj in serializableObjects) {
            try {
                obj.LoadFromBinary(reader);
            } catch (Exception ex) {
                logger.LogError("{Message}.", ex.Message);
                errors = true;
            }
        }
        return !errors;
    }

    private static bool WriteSerializableObject(IBinarySerializable serializableObject, BinaryWriter writer, ILogger logger)
    {
        try {
            SerializedData[] saveData = serializableObject.GetBinarySerialData();
            foreach (SerializedData saveDatum in saveData) {
                if (saveDatum.WriteTypeName)
                    if (saveDatum.SerialValues.Length > 1)
                        WriteConvertiblesWithType(writer, saveDatum.SerialType, saveDatum.SerialValues);
                    else
                        WriteConvertibleWithType(writer, saveDatum.SerialType, saveDatum.SerialValues[0]);
                else
                    if (saveDatum.SerialValues.Length > 1)
                    WriteConvertibles(writer, saveDatum.SerialType, saveDatum.SerialValues);
                else
                    WriteConvertible(writer, saveDatum.SerialType, saveDatum.SerialValues[0]);
            }
        } catch (Exception ex) {
            logger.LogError("{Message}.", ex.Message);
            return false;
        }
        return true;
    }
}
