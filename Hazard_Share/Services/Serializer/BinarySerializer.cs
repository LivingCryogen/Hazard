using Hazard_Share.Interfaces.Model;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Hazard_Share.Services.Serializer;

/// <summary>
/// A binary serializer for <see cref="IBinarySerializable"/> objects. 
/// </summary>
/// <remarks>
/// <para><see cref="BinarySerializer"/> and <see cref="IBinarySerializable"/> work with <see cref="SerializedData"/>, which requires <see cref="IConvertible"/> values.
/// This serializer encodes <see cref="IConvertible"/> values, with un-encoded optional <see cref="string">tags</see>.</para>
/// <para>To read back tagged values, (assuming BinaryReader reader):
/// <example><code>var readTag = reader.readString();<br/>
/// var readValue = (T)BinarySerializer.ReadConvertible(reader, typeof(T));</code><br/>
/// While reading back untagged values requires only:
/// <code>var readValue = (T)BinarySerializer.ReadConvertible(reader, typeof(T));</code></example></para>
/// </remarks>
public static class BinarySerializer
{
    private static ILogger? _logger;
    /// <summary>
    /// Initializes this <see cref="BinarySerializer"/>'s <see cref="ILogger"/>.
    /// </summary>
    /// <param name="loggerFactory">An <see cref="ILoggerFactory"/> configured to create the appropriate <see cref="ILogger"/>.</param>
    public static void InitializeLogger(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(typeof(BinarySerializer));
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
    /// <summary>
    /// Reads an <see cref="IConvertible"/> value previously written using <see cref="BinarySerializer"/>.
    /// </summary>
    /// <param name="reader">A <see cref="BinaryReader"/> whose <see cref="BinaryReader.BaseStream"/> was previously written to.</param>
    /// <param name="type">The final<see cref="Type"/> expected.</param>
    /// <returns>An <see cref="IConvertible"/> whose <see cref="Type"/> matches <paramref name="type"/>.</returns>
    /// <remarks><paramref name="type"/> is compatible with any <see cref="IConvertible"/>.</remarks>
    public static IConvertible ReadConvertible(BinaryReader reader, Type type)
    {
        int length = reader.ReadInt32();
        byte[] bytes = reader.ReadBytes(length);
        return BytesToConvertible(type, bytes);
    }
    private static object ReadEnum(BinaryReader reader, Type type)
    {
        int length = reader.ReadInt32();
        byte[] bytes = reader.ReadBytes(length);
        return BytesToEnumObject(type, bytes);
    }
    /// <summary>
    /// Reads an array of <see cref="IConvertible"/> values previously written using <see cref="BinarySerializer"/>.
    /// </summary>
    /// <param name="reader">A <see cref="BinaryReader"/> whose <see cref="BinaryReader.BaseStream"/> was previously written to.</param>
    /// <param name="type">The underlying <see cref="Type"/> of the expected <see cref="Array"/>. Must be an <see cref="IConvertible"/> other than <see cref="string"/> and <see cref="Enum"/>.</param>
    /// <param name="numValues">The <see cref="int">number</see> of values expected in the read <see cref="Array"/>.</param>
    /// <returns>An <see cref="Array"/> of <see cref="IConvertible"/>s, exlcuding <see cref="string"/> and <see cref="Enum"/>, whose <see cref="Type"/> matches <paramref name="type"/>.</returns>
    /// <remarks>
    /// <see cref="Array"/>s of type <see cref="string"/> and <see cref="Enum"/> require <see cref="ReadStrings(BinaryReader, Type, int)"/> and <see cref="ReadEnums(BinaryReader, Type, int)"/>, respectively.
    /// </remarks>
    /// <exception cref="ArgumentException">.</exception>
    public static Array ReadConvertibles(BinaryReader reader, Type type, int numValues)
    {
        if (!typeof(IConvertible).IsAssignableFrom(type) || type.IsEnum || type == typeof(string))
            throw new ArgumentException("ReadConvertibles accepts only IConvertible types, excluding strings and Enums.", nameof(type));

        Array returnArray = Array.CreateInstance(type, numValues);
        for (int i = 0; i < numValues; i++)
            returnArray.SetValue(ReadConvertible(reader, type), i);
        return returnArray;
    }
    /// <summary>
    /// Reads an array of <see cref="string"/> values previously written using <see cref="BinarySerializer"/>.
    /// </summary>
    /// <param name="reader">A <see cref="BinaryReader"/> whose <see cref="BinaryReader.BaseStream"/> was previously written to.</param>
    /// <param name="numValues"></param>
    /// <returns>An <see cref="Array"/> of <see cref="string"/>s.</returns>
    public static Array ReadStrings(BinaryReader reader, int numValues)
    {
        Array returnArray = Array.CreateInstance(typeof(string), numValues);
        for (int i = 0; i < numValues; i++)
            returnArray.SetValue((string)ReadConvertible(reader, typeof(string)), i);
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
    public static bool Load(IBinarySerializable[] serializableObjects, string fileName, long streamLoc)
    {
        using FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read);
        fileStream.Position = streamLoc;
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
