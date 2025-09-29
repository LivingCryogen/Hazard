using Microsoft.Extensions.Logging;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats;

public class SavedStatMetadata(ILogger<SavedStatMetadata> logger) : IBinarySerializable
{
    private readonly ILogger<SavedStatMetadata> _logger = logger;

    public string? SavePath { get; set; }
    public long? StreamPosition { get; set; }
    public int ActionCount { get; set; }
    public bool SyncPending { get; set; }

    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(() =>
        {
            List<SerializedData> saveData = [];
            int numPath = string.IsNullOrEmpty(SavePath) ? 0 : 1;
            saveData.Add(new(typeof(int), numPath));
            if (numPath > 0)
                saveData.Add(new(typeof(string), numPath));
            int numStreamLoc = StreamPosition == null || StreamPosition == 0 ? 0 : 1;
            saveData.Add(new(typeof(int), numStreamLoc));
            if (numStreamLoc > 0)
                saveData.Add(new(typeof(long), StreamPosition!));
            saveData.Add(new(typeof(int), ActionCount));
            saveData.Add(new(typeof(bool), SyncPending));
            return saveData.ToArray();
        });
    }

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
