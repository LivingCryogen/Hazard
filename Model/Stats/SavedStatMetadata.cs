using Microsoft.Extensions.Logging;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats;

/// <summary>
/// Provides statistics repository with metadata about the saved stats file, such as path, stream position, action count, and sync status.
/// </summary>
/// <param name="logger"></param>
public class SavedStatMetadata(ILogger<SavedStatMetadata> logger) : IBinarySerializable
{
    private readonly ILogger<SavedStatMetadata> _logger = logger;

    /// <summary>
    /// Gets or sets the path to the saved stats file, if any.
    /// </summary>
    /// <value>
    /// The path to the saved stats file, or <see langword="null"/> if no path is set.
    /// </value>
    public string? SavePath { get; set; }
    /// <summary>
    /// Gets or sets the position in the stream where the StatTracker write began, if any.
    /// </summary>
    /// <remarks>
    /// See <see cref="IBinarySerializable"/> and <see cref="BinarySerializer.Load(IBinarySerializable[], string, long, out long)"/>."/>
    /// </remarks>
    public long? StreamPosition { get; set; }
    /// <summary>
    /// Gets or sets the number of actions recorded in the file.
    /// </summary>
    public int ActionCount { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether there are unsynchronized actions on disk that have not yet been uploaded to the Azure database.
    /// </summary>
    public bool SyncPending { get; set; }
    /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>
    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(() =>
        {
            List<SerializedData> saveData = [];
            int numPath = string.IsNullOrEmpty(SavePath) ? 0 : 1;
            saveData.Add(new(typeof(int), numPath));
            if (numPath > 0)
                saveData.Add(new(typeof(string), SavePath!));
            int numStreamLoc = StreamPosition == null || StreamPosition == 0 ? 0 : 1;
            saveData.Add(new(typeof(int), numStreamLoc));
            if (numStreamLoc > 0)
                saveData.Add(new(typeof(long), StreamPosition!));
            saveData.Add(new(typeof(int), ActionCount));
            saveData.Add(new(typeof(bool), SyncPending));
            return saveData.ToArray();
        });
    }
    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
    public bool LoadFromBinary(BinaryReader reader)
    {
        try
        {
            int numPath = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            if (numPath > 0)
                SavePath = (string)BinarySerializer.ReadConvertible(reader, typeof(string));
            int numStreamLoc = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            if (numStreamLoc > 0)
                StreamPosition = (long)BinarySerializer.ReadConvertible(reader, typeof(long));
            ActionCount = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            SyncPending = (bool)BinarySerializer.ReadConvertible(reader, typeof(bool));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("There was an unexpected error when attempting to load SaveStatMetaData for stats in {Path}: {Message}", SavePath ?? "no path!", ex.Message);
            return false;
        }
    }
}
