using Microsoft.Extensions.Logging;
using Shared.Interfaces.Model;
using System.Collections;
using System.Text;
using System.IO;

namespace Shared.Services.Serializer;

/// <summary>
/// A binary serializer for <see cref="IBinarySerializable"/> objects. 
/// </summary>
/// <remarks>
/// <para><see cref="BinarySerializer"/> and <see cref="IBinarySerializable"/> work with <see cref="SerializedData"/>, which requires <see cref="IConvertible"/> values.<br/>
/// It encodes <see cref="IConvertible"/> values with optional un-encoded <see cref="string">tags</see>.</para>
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
    /// Initializes the logger using a factory.
    /// </summary>
    /// <param name="loggerFactory">A logger factory configured to create the appropriate <see cref="ILogger"/>.</param>
    public static void InitializeLogger(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(typeof(BinarySerializer));
    }
    /// <summary>
    /// Initializes the logger.
    /// </summary>
    /// <param name="logger">The logger.</param>
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
    /// <summary>
    /// Reads a value previously written as an IConvertible using <see cref="BinarySerializer"/>.
    /// </summary>
    /// <param name="reader">A reader whose <see cref="BinaryReader.BaseStream"/> was previously written to.</param>
    /// <param name="type">The final type expected. Must implement IConvertible.</param>
    /// <returns>An IConvertible of type <paramref name="type"/>.</returns>
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
    /// Reads an array of values previously written as <see cref="IConvertible"/>s using <see cref="BinarySerializer"/>.
    /// </summary>
    /// <param name="reader">A reader whose <see cref="BinaryReader.BaseStream"/> was previously written to.</param>
    /// <param name="type">The underlying type of the expected array. Must implement <see cref="IConvertible"/>, excluding <see cref="string"/> and <see cref="Enum"/>.</param>
    /// <param name="numValues">The number of values expected.</param>
    /// <returns><see cref="IConvertible"/>s of type <paramref name="type"/>, excluding <see cref="string"/> and <see cref="Enum"/>.</returns>
    /// <remarks>
    /// <see cref="Array"/>s of type <see cref="string"/> and <see cref="Enum"/> require <see cref="ReadStrings(BinaryReader, int)"/> and <see cref="ReadEnums(BinaryReader, Type, int)"/>, respectively.
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
    /// Reads an array of string values previously written using <see cref="BinarySerializer"/>.
    /// </summary>
    /// <param name="reader">A reader whose <see cref="BinaryReader.BaseStream"/> was previously written to.</param>
    /// <param name="numValues">The number of values in the array (ie: length).</param>
    /// <returns>An <see cref="Array"/> of <see cref="string"/>.</returns>
    public static Array ReadStrings(BinaryReader reader, int numValues)
    {
        Array returnArray = Array.CreateInstance(typeof(string), numValues);
        for (int i = 0; i < numValues; i++)
            returnArray.SetValue((string)ReadConvertible(reader, typeof(string)), i);
        return returnArray;
    }
    /// <summary>
    /// Reads an array of Enum values which were previously written using <see cref="BinarySerializer"/>.
    /// </summary>
    /// <param name="reader">A reader whose <see cref="BinaryReader.BaseStream"/> was previously written to.</param>
    /// <param name="type">The type of Enum to be read. Should be the same as was previously written.</param>
    /// <param name="numValues">The number of values in the array (ie: length).</param>
    /// <returns>An <see cref="Array"/> of <see cref="Enum"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="type"/> is not an <see cref="Enum"/>.</exception>
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
    /// <summary>
    /// Serializes <see cref="IBinarySerializable"/> objects and writes them to a file. 
    /// </summary>
    /// <param name="serializableObjects">The objects to serialize and save.</param>
    /// <param name="fileName">The name of the save file.</param>
    /// <param name="newFile">A flag indicating whether the file is a new file.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    /// <remarks>
    /// See <see cref="WriteSerializableObject"/> and <see cref="IBinarySerializable.GetBinarySerials()"/>.
    /// </remarks>
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
    /// <summary>
    /// Loads <see cref="IBinarySerializable"/> objects with deserialized values previously written by <see cref="BinarySerializer"/>.
    /// </summary>
    /// <param name="serializableObjects">The objects to load.</param>
    /// <param name="fileName">The name of the file to read.</param>
    /// <returns><see langword="true"/> if deserialization and loading completed without exceptions; otherwise, <see langword="false"/>.</returns>
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
    /// <inheritdoc cref="Load(IBinarySerializable[], string)"/>
    /// <param name="serializableObjects">An array of serializable objects to load.</param>
    /// <param name="fileName">The nameof the file to read from.</param>
    /// <param name="startStreamPosition">The position of the <see cref="FileStream"/> at which to begin reading the file.</param>
    /// <param name="endStreamPosition">The position of the <see cref="FileStream"/> after loading is complete.</param>
    public static bool Load(IBinarySerializable[] serializableObjects, string fileName, long startStreamPosition, out long endStreamPosition)
    {
        using FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read);
        fileStream.Position = startStreamPosition;
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
        endStreamPosition = fileStream.Position;
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
                if (saveDatum.MemberType is not null) {
                    if (saveDatum.Tag != null)
                        WriteTaggedConvertibles(writer, saveDatum.MemberType, saveDatum.SerialValues, saveDatum.Tag);
                    else
                        WriteConvertibles(writer, saveDatum.MemberType, saveDatum.SerialValues);
                }
                else {
                    if (saveDatum.Tag != null)
                        WriteTaggedConvertible(writer, saveDatum.SerialType, saveDatum.SerialValues[0], saveDatum.Tag);
                    else
                        WriteConvertible(writer, saveDatum.SerialType, saveDatum.SerialValues[0]);
                }
            }
        } catch (Exception ex) {
            _logger?.LogError("{Message}.", ex.Message);
            return false;
        }
        return true;
    }
    /// <summary>
    /// Determines whether objects of a given Type are serializable.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> if objects of type <paramref name="type"/> are serializable by <see cref="BinarySerializer"/>.</returns>
    public static bool IsSerializable(Type type)
    {
        return type switch {
            Type t when t == typeof(string) => true,
            Type t when t.IsEnum => t.GetEnumUnderlyingType() == typeof(int),
            Type t when t.IsPrimitive => true,
            Type t when IsIConvertibleCollection(t, out _) => true,
            _ => false
        };
    }
    /// <summary>
    /// Determines whether a Type is a collection type of IConvertibles.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <param name="memberType">If <paramref name="type"/> is a collection of IConvertibles, the type of its members; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the collection is a collection of IConvertibles; otherwise, <see langword="false"/>.</returns>
    public static bool IsIConvertibleCollection(Type type, out Type? memberType)
    {
        if (!typeof(IEnumerable).IsAssignableFrom(type) || type == typeof(string)) {
            memberType = null;
            return false;
        }

        // test if an Array contains IConvertible elements
        if (type.IsArray) {
            var elementType = type.GetElementType();
            memberType = elementType;
            return typeof(IConvertible).IsAssignableFrom(elementType);
        }

        // test if a non-generic IEnumerable contains IConvertible elements
        if (!type.IsGenericType) {

            if (Activator.CreateInstance(type) is not IEnumerable testInstance) {
                memberType = null;
                return false;
            }
            Type? elementType = null;
            foreach (var element in testInstance) {
                if (element is not IConvertible) {
                    memberType = null;
                    return false;
                }

                elementType ??= element.GetType();
            }

            memberType = elementType;
            return true;
        }

        // test if a generic IEnumerable -- that must implement IEnumerable<T> -- has IConvertible T
        // Find the IEnumerable<> interface
        var typeInterfaces = type.GetInterfaces();
        Type? genericType = null;
        foreach (var iFace in typeInterfaces) {
            if (iFace.IsGenericType && iFace.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
                genericType = iFace.GetGenericArguments()[0];
                break;
            }
        }
        if (genericType != null) {
            memberType = genericType;
            return typeof(IConvertible).IsAssignableFrom(genericType);
        }
        memberType = null;
        return false;
    }
    /// <summary>
    /// Casts a collection into a collection of IConvertibles.
    /// </summary>
    /// <param name="collection">The collection to cast.</param>
    /// <returns>The cast IConvertibles; if an element could not be successfully cast, it is skipped.</returns>
    public static IConvertible[] ToIConvertibleCollection(IEnumerable collection)
    {
        List<IConvertible> propConvertibles = [];
        foreach (var value in collection) {
            try {
                propConvertibles.Add((IConvertible)value);
            } catch { continue; }
        }
        return [.. propConvertibles];
    }

}
