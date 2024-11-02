using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Model.EventArgs;
using Shared.Geography;
using Shared.Geography.Enums;
using Shared.Interfaces;
using Shared.Interfaces.Model;
using Shared.Services.Serializer;

namespace Model.Entities;
/// <remarks>The default board of the base game is based on Earth circa 1800.</remarks>
/// <inheritdoc cref="IBoard"/>
public class EarthBoard: IBoard, IBinarySerializable
{
    private readonly ILogger<EarthBoard> _logger;
    /// <summary>
    /// Builds an Earth board using configuration '.json' values.
    /// </summary>
    /// <param name="config">An <see cref="IConfiguration"/>. Typically injected by the DI system.</param>
    /// <param name="logger">A logger for debug information and errors.</param>
    public EarthBoard(IConfiguration config, ILogger<EarthBoard> logger)
    {
        _logger = logger;
        Armies = [];
        TerritoryOwner = [];
        for (int i = 0; i < BoardGeography.NumTerritories; i++) {
            Armies.Add((TerrID)i, int.Parse(config["startingArmies"] ?? "0"));
            TerritoryOwner.Add((TerrID)i, -1);
        }

        ContinentOwner = [];
        for (int i = 0; i < BoardGeography.NumContinents; i++)
            ContinentOwner.Add((ContID)i, -1);
    }
    /// <summary>
    /// Gives notice that a territory has changed.
    /// </summary>  
    /// <remarks>Manually fired when a territory is changed (owner or armies) in an <see cref="EarthBoard"/> method.</remarks>    
    public event EventHandler<ITerritoryChangedEventArgs>? TerritoryChanged;
    /// <summary>
    /// Gives notice that a continent has changed.
    /// </summary>  
    /// <remarks>Manually fired when a continent is changed (owner) in an <see cref="EarthBoard"/> method.</remarks>  
    public event EventHandler<IContinentOwnerChangedEventArgs>? ContinentOwnerChanged;

    #region Properties
    /// <summary>
    /// Gets or inits the territory to armies map.
    /// </summary>
    public Dictionary<TerrID, int> Armies { get; init; }
    /// <summary>
    /// Gets or inits the territory to owner (player number) map.
    /// </summary>
    public Dictionary<TerrID, int> TerritoryOwner { get; init; }
    /// <summary>
    /// Gets or inits the continent to owner (player number) map.
    /// </summary>
    public Dictionary<ContID, int> ContinentOwner { get; init; }
    #endregion
    /// <summary>
    /// Gets a list of territories or continents owned by a player.
    /// </summary>
    /// <param name="playerNumber">The <see cref="int">number</see> of the <see cref="IPlayer"/> who owns the territories or continents.</param>
    /// <param name="enumName">The name of <see cref="TerrID"/> or of <see cref="ContID"/>, for territory and continent, respectively.</param>
    /// <returns>A <see cref="List{T}"/> of either <see cref="TerrID"/> or <see cref="ContID"/>.</returns>
    public List<object> this[int playerNumber, string enumName] {
        get {
            if (string.IsNullOrEmpty(enumName)) return [];
            if (enumName == nameof(TerrID))
                return TerritoryOwner
                    .Where(pair => pair.Value == playerNumber)
                    .Select(pair => pair.Key)
                    .Cast<object>()
                    .ToList();
            if (enumName.Equals(nameof(ContID)))
                return ContinentOwner
                    .Where(pair => pair.Value == playerNumber)
                    .Select(pair => pair.Key)
                    .Cast<object>()
                    .ToList();
            return [];
        }
    }

    #region Methods
    /// <inheritdoc cref="IBoard.Claims(int, TerrID)"/>
    public void Claims(int newPlayer, TerrID territory)
    {
        int previousOwner = TerritoryOwner[territory];
        Armies[territory] = 1;
        TerritoryOwner[territory] = newPlayer;
        TerritoryChanged?.Invoke(this, new TerritoryChangedEventArgs(territory, newPlayer));
        CheckContinentFlip(territory, previousOwner);
    }
    /// <inheritdoc cref="IBoard.Claims(int, TerrID, int)"/>
    public void Claims(int newPlayer, TerrID territory, int armies)
    {
        int previousOwner = TerritoryOwner[territory];
        TerritoryOwner[territory] = newPlayer;
        Armies[territory] = armies;
        TerritoryChanged?.Invoke(this, new TerritoryChangedEventArgs(territory, newPlayer));
        CheckContinentFlip(territory, previousOwner);
    }
    /// <inheritdoc cref="IBoard.Reinforce(TerrID)"/>
    public void Reinforce(TerrID territory)
    {
        Armies[territory]++;
        TerritoryChanged?.Invoke(this, new TerritoryChangedEventArgs(territory));
    }
    /// <inheritdoc cref="IBoard.Reinforce(TerrID, int)"/>
    public void Reinforce(TerrID territory, int armies)
    {
        Armies[territory] = Armies[territory] + armies;
        TerritoryChanged?.Invoke(this, new TerritoryChangedEventArgs(territory));
    }
    /// <inheritdoc cref="IBoard.Conquer(TerrID, TerrID, int)"/>
    public void Conquer(TerrID source, TerrID target, int newOwner)
    {
        int previousOwner = TerritoryOwner[target];
        TerritoryOwner[target] = newOwner;
        CheckContinentFlip(target, previousOwner);
        TerritoryChanged?.Invoke(this, new TerritoryChangedEventArgs(target, newOwner));
    }
    /// <inheritdoc cref="IBoard.CheckContinentFlip(TerrID, int)"/>
    public void CheckContinentFlip(TerrID changed, int previousOwner)
    {
        if (changed == TerrID.Null)
            throw new ArgumentException("Non-null TerrID required.", nameof(changed));

        int newOwner = TerritoryOwner[changed];
        var changedHomeContinent = BoardGeography.TerritoryToContinent(changed);
        var continentTerritories = BoardGeography.GetContinentMembers(changedHomeContinent);
        if (continentTerritories == null || continentTerritories.Count <= 0)
            return;

        if (ContinentOwner[changedHomeContinent] == previousOwner && previousOwner > -1) {
            ContinentOwner[changedHomeContinent] = -1;
            if (continentTerritories.All(item => TerritoryOwner[item] == newOwner))
                ContinentOwner[changedHomeContinent] = newOwner;

            ContinentOwnerChanged?.Invoke(this, new ContinentOwnerChangedEventArgs(changedHomeContinent, previousOwner));
        }
        else if (continentTerritories.All(item => TerritoryOwner[item] == newOwner)) {
            ContinentOwner[changedHomeContinent] = newOwner;
            ContinentOwnerChanged?.Invoke(this, new ContinentOwnerChangedEventArgs(changedHomeContinent, previousOwner));
        }
    }
    /// <inheritdoc cref="IBinarySerializable.GetBinarySerials"/>
    public async Task<SerializedData[]> GetBinarySerials()
    {
        return await Task.Run(() =>
        {
            int numCont = ContinentOwner.Count;
            int numTerr = TerritoryOwner.Count;
            int numData = 2 + numCont + (numTerr * 2);
            SerializedData[] saveData = new SerializedData[numData];
            saveData[0] = new(typeof(int), [numCont]);
            saveData[1] = new(typeof(int), [numTerr]);
            int dataIndex = 2;
            for (int i = 0; i < numCont; i++) {
                saveData[dataIndex] = new(typeof(int), [ContinentOwner[(ContID)i]]);
                dataIndex++;
            }
            for (int i = 0; i < numTerr; i++) {
                saveData[dataIndex] = new(typeof(int), [TerritoryOwner[(TerrID)i]]);
                dataIndex++;
                saveData[dataIndex] = new(typeof(int), [Armies[(TerrID)i]]);
                dataIndex++;
            }

            return saveData;
        });
    }
    /// <inheritdoc cref="IBinarySerializable.LoadFromBinary(BinaryReader)"/>
    public bool LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try {
            int numCont = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            int numTerr = (int)BinarySerializer.ReadConvertible(reader, typeof(int));
            ContinentOwner.Clear();
            for (int i = 0; i < numCont; i++)
                ContinentOwner.Add((ContID)i, (int)BinarySerializer.ReadConvertible(reader, typeof(int)));
            TerritoryOwner.Clear();
            Armies.Clear();
            for (int i = 0; i < numTerr; i++) {
                TerritoryOwner.Add((TerrID)i, (int)BinarySerializer.ReadConvertible(reader, typeof(int)));
                Armies.Add((TerrID)i, (int)BinarySerializer.ReadConvertible(reader, typeof(int)));
            }
        } catch (Exception ex) {
            _logger.LogError("An exception was thrown while loading {EarthBoard}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }
    #endregion
}

