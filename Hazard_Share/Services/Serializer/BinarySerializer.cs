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
        foreach (IConvertible value in values)
            WriteConvertible(writer, type, value);
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
        for (int i = 0; i < numValues; i++)
            readConvertibles.Add(ReadConvertible(reader, type));
        return [.. readConvertibles];
    }

    private static void WriteTaggedConvertible(BinaryWriter writer, Type type, IConvertible value, string tag)
    {
        writer.Write(tag);
        WriteConvertible(writer, type, value);
    }
    private static void WriteTaggedConvertibles(BinaryWriter writer, Type type, IConvertible[] values, string tag)
    {
        writer.Write(tag);
        writer.Write(values.Length);
        foreach (IConvertible value in values)
            WriteTaggedConvertible(writer, type, value, tag);
    }
    private static IConvertible ReadTaggedConvertible(BinaryReader reader, Type type, int numValues, out string tag)
    {
        tag = reader.ReadString();
        return ReadConvertible(reader, type);
    }
    private static IConvertible[] ReadTaggedConvertibles(BinaryReader reader, Type type, out string tag)
    {
        tag = reader.ReadString();
        int numTagged = reader.ReadInt32();
        return ReadConvertibles(reader, type, numTagged);
    }
    private static IConvertible[] ReadTaggedConvertibles(BinaryReader reader, Type type, int numValues, out string tag)
    {
        tag = reader.ReadString();
        int numTagged = reader.ReadInt32();
        if (numValues != numTagged)
            throw new ArgumentException($"A number of items {numValues} was expected, which did not match the number of tags read. ", nameof(numValues));
        return ReadConvertibles(reader, type, numValues);
    }
    #endregion

    public async static Task Save(IBinarySerializable[] serializableObjects, string fileName, bool newFile, ILogger logger)
    {
        await Task.Run(() =>
        {
            if (newFile) {
                using FileStream fileStream = new(fileName, FileMode.Create, FileAccess.Write);
                using BinaryWriter writer = new(fileStream);

                foreach (var obj in serializableObjects)
                    try {
                        if (!WriteSerializableObject(obj, writer, logger))
                            logger.LogWarning("BinarySerializer failed to write {Object}.", obj);
                    } catch (Exception e) {
                        logger.LogError("An exception was thrown when attempting to write {obj}: {Message}.", obj, e.Message);
                    }
            }
            else {
                using FileStream fileStream = new(fileName, FileMode.Truncate, FileAccess.Write);
                using BinaryWriter writer = new(fileStream);


                foreach (var obj in serializableObjects)
                    try {
                        if (!WriteSerializableObject(obj, writer, logger))
                            logger.LogWarning("BinarySerializer failed to write {Object}.", obj);
                    } catch (Exception e) {
                        logger.LogError("An exception was thrown when attempting to write {obj}: {Message}.", obj, e.Message);
                    }
            }
        });
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
            SerializedData[] saveData = serializableObject.GetBinarySerialData().Result;
            foreach (SerializedData saveDatum in saveData) {
                if (saveDatum.Tag != null) 
                    if (saveDatum.SerialValues.Length > 1)
                        WriteTaggedConvertibles(writer, saveDatum.SerialType, saveDatum.SerialValues, saveDatum.Tag);
                    else
                        WriteTaggedConvertible(writer, saveDatum.SerialType, saveDatum.SerialValues[0], saveDatum.Tag);
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
