using Microsoft.Extensions.Logging;
using Model.Tests.Fixtures.Stubs;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Tests.Stats;

public class MockPlayerStats
{
    private readonly ILogger _logger = new LoggerStubT<MockPlayerStats>();

    public string Name { get; set; } = string.Empty;
    public int Number { get; set; } = 0;
    public int ContinentsConquered { get; set; } = 0;
    public int AttacksWon { get; set; } = 0;
    public int AttacksLost { get; set; } = 0;
    public int Conquests { get; set; } = 0;
    public int Retreats { get; set; } = 0; // forced to retreat while attacking
    public int ForcedRetreats { get; set; } = 0; // while defending, forced attacker to retreat
    public int Moves { get; set; } = 0;
    public int MaxAdvances { get; set; } = 0;
    public int TradeIns { get; set; } = 0;
    public int TotalOccupationBonus { get; set; } = 0;

    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(() =>
        {
            List<SerializedData> saveData = [];
            saveData.Add(new(typeof(string), Name));
            saveData.Add(new(typeof(int), Number));
            saveData.Add(new(typeof(int), ContinentsConquered));
            saveData.Add(new(typeof(int), AttacksWon));
            saveData.Add(new(typeof(int), AttacksLost));
            saveData.Add(new(typeof(int), Conquests));
            saveData.Add(new(typeof(int), Retreats));
            saveData.Add(new(typeof(int), ForcedRetreats));
            saveData.Add(new(typeof(int), Moves));
            saveData.Add(new(typeof(int), MaxAdvances));
            saveData.Add(new(typeof(int), TradeIns));
            saveData.Add(new(typeof(int), TotalOccupationBonus));
            return saveData.ToArray();
        });
    }

    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try
        {
            Name = (string)BinarySerializer.ReadConvertible(reader, typeof(string));
            Number = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            ContinentsConquered = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            AttacksWon = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            AttacksLost = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            Conquests = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            Retreats = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            ForcedRetreats = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            Moves = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            MaxAdvances = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            TradeIns = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            TotalOccupationBonus = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
        }
        catch (Exception ex)
        {
            _logger.LogError("An exception was thrown while loading {PlayerStats}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
}

