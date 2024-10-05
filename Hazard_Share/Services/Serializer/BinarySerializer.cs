using Hazard_Share.Interfaces.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hazard_Share.Services.Serializer;

public static class BinarySerializer
{
    private static ILogger? _logger;
    public static void InitializeLogger(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(typeof(BinarySerializer));
    }
    public static void InitializeLogger(ILogger logger)
    {
        _logger = logger;
    }

    #region Encoding Methods
    private static byte[] ConvertibleToBytes(Type type, IConvertible value)
    {
        if (type == typeof(byte))
            return [(byte)value];
        else if (type == typeof(string))
            return Encoding.UTF8.GetBytes((string)value);
        else if (type.IsEnum)
            return BitConverter.GetBytes(Convert.ToInt64(value));
        else
            return BitConverter.GetBytes(Convert.ToDouble(value));
    }
    private static IConvertible BytesToConvertible(Type type, byte[] bytes)
    {
        if (type == typeof(byte) && bytes.Length == 1)
            return bytes[0];
        if (type == typeof(string))
            return Encoding.UTF8.GetString(bytes);
        if (type.IsEnum)
            return (IConvertible)Enum.ToObject(type, BitConverter.ToInt64(bytes, 0));

        double doubleVal = BitConverter.ToDouble(bytes, 0);
        return (IConvertible)Convert.ChangeType(doubleVal, type);
    }
    private static object BytesToEnumObject(Type type, byte[] bytes)
    {
        return Enum.ToObject(type, BitConverter.ToInt64(bytes, 0));
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
        foreach (IConvertible value in values)
            WriteConvertible(writer, type, value);
    }
    public static IConvertible ReadConvertible(BinaryReader reader, Type type)
    {
        int length = reader.ReadInt32();
        byte[] bytes = reader.ReadBytes(length);
        return BytesToConvertible(type, bytes);
    }
    public static object ReadEnum(BinaryReader reader, Type type)
    {
        int length = reader.ReadInt32();
        byte[] bytes = reader.ReadBytes(length);
        return BytesToEnumObject(type, bytes);
    }
    public static Array ReadConvertibles(BinaryReader reader, Type type, int numValues)
    {
        if (!typeof(IConvertible).IsAssignableFrom(type))
            throw new ArgumentException("ReadConvertibles accepts only types which implement IConvertible.", nameof(type));

        Array returnArray = Array.CreateInstance(type, numValues);
        for (int i = 0; i < numValues; i++)
            returnArray.SetValue(ReadConvertible(reader, type), i);
        return returnArray;
    }
    public static Array ReadStrings(BinaryReader reader, Type type, int numValues)
    {
        if (!type.IsAssignableFrom(typeof(string)))
            throw new ArgumentException("ReadConvertibles accepts only string types.", nameof(type));

        Array returnArray = Array.CreateInstance(type, numValues);
        for (int i = 0; i < numValues; i++)
            returnArray.SetValue((string)ReadConvertible(reader, type), i);
        return returnArray;
    }
    public static Array ReadEnums(BinaryReader reader, Type type, int numValues)
    {
        if (!type.IsEnum)
            throw new ArgumentException("ReadEnums accepts only Enum types.", nameof(type));

        Array returnArray = Array.CreateInstance(type, numValues);
        for (int i = 0; i < numValues; i++)
            returnArray.SetValue(ReadEnum(reader, type), i);
        return returnArray;
    }

    private static void WriteTaggedConvertible(BinaryWriter writer, Type type, IConvertible value, string tag)
    {
        writer.Write(tag);
        WriteConvertible(writer, type, value);
    }
    private static void WriteTaggedConvertibles(BinaryWriter writer, Type type, IConvertible[] values, string tag)
    {
        writer.Write(tag);
        WriteConvertibles(writer, type, values);
    }
    
    #endregion

    public async static Task Save(IBinarySerializable[] serializableObjects, string fileName, bool newFile)
    {
        await Task.Run(() =>
        {
            if (newFile) {
                using FileStream fileStream = new(fileName, FileMode.Create, FileAccess.Write);
                using BinaryWriter writer = new(fileStream);

                foreach (var obj in serializableObjects)
                    try {
                        if (!WriteSerializableObject(obj, writer).Result)
                            _logger?.LogWarning("BinarySerializer failed to write {Object}.", obj);
                    } catch (Exception e) {
                        _logger?.LogError("An exception was thrown when attempting to write {obj}: {Message}.", obj, e.Message);
                    }
            }
            else {
                using FileStream fileStream = new(fileName, FileMode.Truncate, FileAccess.Write);
                using BinaryWriter writer = new(fileStream);

                foreach (var obj in serializableObjects)
                    try {
                        if (!WriteSerializableObject(obj, writer).Result)
                            _logger?.LogWarning("BinarySerializer failed to write {Object}.", obj);
                    } catch (Exception e) {
                        _logger?.LogError("An exception was thrown when attempting to write {obj}: {Message}.", obj, e.Message);
                    }
            }
        });
    }
    public static bool Load(IBinarySerializable[] serializableObjects, string fileName)
    {
        using FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(fileStream);

        bool errors = false;
        foreach (var obj in serializableObjects) {
            try {
                obj.LoadFromBinary(reader);
            } catch (Exception ex) {
                _logger?.LogError("{Message}.", ex.Message);
                errors = true;
            }
        }
        return !errors;
    }

    private async static Task<bool> WriteSerializableObject(IBinarySerializable serializableObject, BinaryWriter writer)
    {
        try {
            if (await serializableObject.GetBinarySerials() is not SerializedData[] saveData) {
                _logger?.LogError("BinarySerializer failed to write {object} because it did not return a valid SerializedData[].", serializableObject);
                return false;
            }
            foreach (SerializedData saveDatum in saveData) {
                if (saveDatum.Tag != null) 
                    if (saveDatum.SerialValues.Length == 1)
                        WriteTaggedConvertible(writer, saveDatum.SerialType, saveDatum.SerialValues[0], saveDatum.Tag);
                    else
                        WriteTaggedConvertibles(writer, saveDatum.SerialType, saveDatum.SerialValues, saveDatum.Tag);
                else 
                    if (saveDatum.SerialValues.Length > 1)
                        WriteConvertibles(writer, saveDatum.SerialType, saveDatum.SerialValues);
                    else if (saveDatum.SerialValues.Length == 1)
                        WriteConvertible(writer, saveDatum.SerialType, saveDatum.SerialValues[0]);
            }
        } catch (Exception ex) {
            _logger?.LogError("{Message}.", ex.Message);
            return false;
        }
        return true;
    }
}
