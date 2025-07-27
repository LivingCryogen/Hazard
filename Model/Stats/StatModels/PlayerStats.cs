using Microsoft.Extensions.Logging;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats.StatModels;
/// <summary>
/// A model for player statistics generated during a <see cref="GameSession"/>.
/// </summary>
/// <param name="logger">A logger for logging error information, provided by DI or a factory.</param>
public class PlayerStats(ILogger<PlayerStats> logger) : IBinarySerializable
{
    private readonly ILogger _logger = logger;
    /// <summary>
    /// Gets or sets the name of the player.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the number of the player.
    /// </summary>
    public int Number { get; set; } = 0;
    /// <summary>
    /// Gets or sets the number of continents conquered by the player.
    /// </summary>
    public int ContinentsConquered { get; set; } = 0;
    /// <summary>
    /// Gets or sets the number of attacks won by the player.
    /// </summary>
    public int AttacksWon { get; set; } = 0;
    /// <summary>
    /// Gets or sets the number of attacks lost by the player.
    /// </summary>
    public int AttacksLost { get; set; } = 0;
    /// <summary>
    /// Gets or sets the number of conquests made by the player.
    /// </summary>
    public int Conquests { get; set; } = 0;
    /// <summary>
    /// Gets or sets the number of times the player was forced to retreat when attacking.
    /// </summary>
    public int Retreats { get; set; } = 0;
    /// <summary>
    /// Gets or sets the number of times the player forced an attacker to retreat while defending.
    /// </summary>
    public int ForcedRetreats { get; set; } = 0;
    /// <summary>
    /// Gets or sets the number of moves made by the player.
    /// </summary>
    public int Moves { get; set; } = 0;
    /// <summary>
    /// Gets or sets the number of maximum advances made by the player (moves in which the maximum possible number of armies was chosen). <br/>
    /// This applies to both Move and Attack phases (ie, Advances post Conquest).
    /// </summary>
    public int MaxAdvances { get; set; } = 0;
    /// <summary>
    /// Gets or sets the number of trade-ins made by the player.
    /// </summary>
    public int TradeIns { get; set; } = 0;
    /// <summary>
    /// Gets or sets the total of all occupation bonuses received by the player.
    /// </summary>
    public int TotalOccupationBonus { get; set; } = 0;
    /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>"
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
    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
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






