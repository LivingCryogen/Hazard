using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Model.EventArgs;
using Share.Enums;
using Share.Interfaces;
using Share.Interfaces.Model;
using Share.Services.Serializer;
using System.Collections.ObjectModel;

namespace Model.Entities;
/// <remarks>The default board of the base game is based on Earth circa 1800.</remarks>
/// <inheritdoc cref="IBoard"/>
public class EarthBoard : IBoard, IBinarySerializable
{
    private readonly ILogger<EarthBoard> _logger;
    /// <remarks>
    /// <para> Currently the Graph of territories to their neighbors is implemented naively with Dictionaries. This is just fine for small boards <br/>
    /// (like the default), but to enable larger possible options, as well as to gain experience with the actual Graph class, this may <br/>
    /// be refactored in the future. </para>
    /// <para> Similarly, the values for everything here are currently hard-coded, but, like <see cref="ICardSet"/>s and their <see cref="ICard"/>s, <br/>
    /// loading '.json' asset files through the DAL for these values is the preferred method. This may be implemented in the near future. </para>
    /// </remarks>
    /// <inheritdoc cref="IBoard.Geography"/>
    public class EarthGeography : IGeography
    {
        /// <summary>
        /// A list of territories (<see cref="TerrID"/>) within the continent of "NorthAmerica"
        /// </summary>
        public static readonly List<TerrID> NorthAmerica = [
            TerrID.Alaska,
            TerrID.NorthwestTerritory,
            TerrID.Greenland,
            TerrID.Alberta,
            TerrID.Ontario,
            TerrID.Quebec,
            TerrID.WesternUnitedStates,
            TerrID.EasternUnitedStates,
            TerrID.CentralAmerica
        ];
        /// <summary>
        /// A list of territories (<see cref="TerrID"/>) within the continent of "SouthAmerica"
        /// </summary>
        public static readonly List<TerrID> SouthAmerica = [
            TerrID.Venezuela,
            TerrID.Peru,
            TerrID.Brazil,
            TerrID.Argentina
        ];
        /// <summary>
        /// A list of territories within (<see cref="TerrID"/>) the continent of "Europe"
        /// </summary>
        public static readonly List<TerrID> Europe = [
            TerrID.Iceland,
            TerrID.Scandinavia,
            TerrID.Ukraine,
            TerrID.GreatBritain,
            TerrID.NorthernEurope,
            TerrID.WesternEurope,
            TerrID.SouthernEurope
        ];
        /// <summary>
        /// A list of territories within (<see cref="TerrID"/>) the continent of "Africa"
        /// </summary>
        public static readonly List<TerrID> Africa = [
            TerrID.NorthAfrica,
            TerrID.Egypt,
            TerrID.EastAfrica,
            TerrID.Congo,
            TerrID.SouthAfrica,
            TerrID.Madagascar
        ];
        /// <summary>
        /// A list of territories within (<see cref="TerrID"/>) the continent of "Asia"
        /// </summary>
        public static readonly List<TerrID> Asia = [
            TerrID.Ural,
            TerrID.Siberia,
            TerrID.Yakutsk,
            TerrID.Kamchatka,
            TerrID.Irkutsk,
            TerrID.Afghanistan,
            TerrID.China,
            TerrID.Mongolia,
            TerrID.Japan,
            TerrID.MiddleEast,
            TerrID.India,
            TerrID.Siam
        ];
        /// <summary>
        /// A list of territories within (<see cref="TerrID"/>) the continent of "Oceania"
        /// </summary>
        public static readonly List<TerrID> Oceania = [
            TerrID.Indonesia,
            TerrID.NewGuinea,
            TerrID.WesternAustralia,
            TerrID.EasternAustralia
        ];
        /// <summary>
        /// Gets the total number of territories in this <see cref="Geography"/>.
        /// </summary>
        /// <value>A positive <see langword="int"/>.</value>
        public int NumTerritories { get; } = 42;
        /// <summary>
        /// Gets the total number of continents in this <see cref="Geography"/>.
        /// </summary>
        /// <value>A positive <see langword="int"/>.</value>
        public int NumContinents { get; } = 6;
        /// <summary>
        /// Gets a mapping of continents to the territories they contain.
        /// </summary>
        /// <value>A <see cref="ReadOnlyDictionary{TKey, TValue}"/> keyed by <see cref="ContID"/> to <see cref="List{T}"/> of <see cref="TerrID"/>.</value>
        public static ReadOnlyDictionary<ContID, List<TerrID>> ContinentMembers { get; } = new(new Dictionary<ContID, List<TerrID>>()
            {
                { ContID.NorthAmerica, NorthAmerica },
                { ContID.SouthAmerica, SouthAmerica },
                { ContID.Europe, Europe },
                { ContID.Africa, Africa },
                { ContID.Asia, Asia },
                { ContID.Oceania, Oceania }
            });
        /// <summary>
        /// Gets a mapping of territories to the their neighbors.
        /// </summary>
        /// <remarks>
        /// A naive implementation of a Graph, this is performant enough for small graphs and search depths (~2). But it should be replaced <br/>
        /// if an extension or modification will require signifacantly large graphs or deeper searches.
        /// </remarks>
        /// <value>
        /// A <see cref="ReadOnlyDictionary{TKey, TValue}"/> keyed by <see cref="TerrID"/> to <see cref="List{T}"/> of <see cref="TerrID"/>.
        /// </value>
        public ReadOnlyDictionary<TerrID, List<TerrID>> NeighborWeb { get; } = new(new Dictionary<TerrID, List<TerrID>>
        {
                { TerrID.Alaska, new List<TerrID>
                    {
                        TerrID.NorthwestTerritory,
                        TerrID.Alberta,
                        TerrID.Kamchatka
                    }
                },

                { TerrID.NorthwestTerritory, new List<TerrID>
                    {
                        TerrID.Alaska,
                        TerrID.Greenland,
                        TerrID.Alberta,
                        TerrID.Ontario
                    }
                },

                { TerrID.Greenland, new List<TerrID>
                    {
                        TerrID.NorthwestTerritory,
                        TerrID.Iceland,
                        TerrID.Ontario,
                        TerrID.Quebec
                    }
                },

                { TerrID.Alberta, new List<TerrID>
                    {
                        TerrID.Alaska,
                        TerrID.NorthwestTerritory,
                        TerrID.Ontario,
                        TerrID.WesternUnitedStates
                    }
                },

                { TerrID.Ontario, new List<TerrID>
                    {
                        TerrID.NorthwestTerritory,
                        TerrID.Greenland,
                        TerrID.Quebec,
                        TerrID.EasternUnitedStates,
                        TerrID.WesternUnitedStates,
                        TerrID.Alberta
                    }
                },

                { TerrID.Quebec, new List<TerrID>
                    {
                        TerrID.Greenland,
                        TerrID.EasternUnitedStates,
                        TerrID.Ontario
                    }
                },

                { TerrID.WesternUnitedStates, new List<TerrID>
                    {
                        TerrID.Alberta,
                        TerrID.Ontario,
                        TerrID.EasternUnitedStates,
                        TerrID.CentralAmerica
                    }
                },

                { TerrID.EasternUnitedStates, new List<TerrID>
                    {
                        TerrID.Ontario,
                        TerrID.Quebec,
                        TerrID.CentralAmerica,
                        TerrID.WesternUnitedStates
                    }
                },

                { TerrID.CentralAmerica, new List<TerrID>
                    {
                        TerrID.WesternUnitedStates,
                        TerrID.EasternUnitedStates,
                        TerrID.Venezuela
                    }
                },

                { TerrID.Venezuela, new List<TerrID>
                    {
                        TerrID.CentralAmerica,
                        TerrID.Peru,
                        TerrID.Brazil
                    }
                },

                { TerrID.Peru, new List<TerrID>
                    {
                        TerrID.Venezuela,
                        TerrID.Brazil,
                        TerrID.Argentina
                    }
                },

                { TerrID.Brazil, new List<TerrID>
                    {
                        TerrID.Venezuela,
                        TerrID.NorthAfrica,
                        TerrID.Peru,
                        TerrID.Argentina
                    }
                },

                { TerrID.Argentina, new List<TerrID>
                    {
                        TerrID.Peru,
                        TerrID.Brazil,
                    }
                },

                { TerrID.Iceland, new List<TerrID>
                    {
                        TerrID.Greenland,
                        TerrID.Scandinavia,
                        TerrID.GreatBritain
                    }
                },

                { TerrID.Scandinavia, new List<TerrID>
                    {
                        TerrID.Iceland,
                        TerrID.GreatBritain,
                        TerrID.NorthernEurope,
                        TerrID.Ukraine
                    }
                },

                { TerrID.Ukraine, new List<TerrID>
                    {
                        TerrID.Scandinavia,
                        TerrID.NorthernEurope,
                        TerrID.SouthernEurope,
                        TerrID.MiddleEast,
                        TerrID.Afghanistan,
                        TerrID.Ural
                    }
                },

                { TerrID.GreatBritain, new List<TerrID>
                    {
                        TerrID.Iceland,
                        TerrID.Scandinavia,
                        TerrID.NorthernEurope,
                        TerrID.WesternEurope
                    }
                },

                { TerrID.NorthernEurope, new List<TerrID>
                    {
                        TerrID.Scandinavia,
                        TerrID.Ukraine,
                        TerrID.SouthernEurope,
                        TerrID.WesternEurope,
                        TerrID.GreatBritain
                    }
                },

                { TerrID.WesternEurope, new List<TerrID>
                    {
                        TerrID.GreatBritain,
                        TerrID.NorthernEurope,
                        TerrID.SouthernEurope,
                        TerrID.NorthAfrica
                    }
                },

                { TerrID.SouthernEurope, new List<TerrID>
                    {
                        TerrID.NorthernEurope,
                        TerrID.Ukraine,
                        TerrID.MiddleEast,
                        TerrID.Egypt,
                        TerrID.NorthAfrica,
                        TerrID.WesternEurope
                    }
                },

                { TerrID.NorthAfrica, new List<TerrID>
                    {
                        TerrID.WesternEurope,
                        TerrID.SouthernEurope,
                        TerrID.Egypt,
                        TerrID.EastAfrica,
                        TerrID.Congo,
                        TerrID.Brazil
                    }
                },

                { TerrID.Egypt, new List<TerrID>
                    {
                        TerrID.SouthernEurope,
                        TerrID.MiddleEast,
                        TerrID.EastAfrica,
                        TerrID.NorthAfrica
                    }
                },

                { TerrID.EastAfrica, new List<TerrID>
                    {
                        TerrID.Egypt,
                        TerrID.MiddleEast,
                        TerrID.Madagascar,
                        TerrID.SouthAfrica,
                        TerrID.Congo,
                        TerrID.NorthAfrica
                    }
                },

                { TerrID.Congo, new List<TerrID>
                    {
                        TerrID.NorthAfrica,
                        TerrID.EastAfrica,
                        TerrID.SouthAfrica
                    }
                },

                { TerrID.SouthAfrica, new List<TerrID>
                    {
                        TerrID.Congo,
                        TerrID.EastAfrica,
                        TerrID.Madagascar
                    }
                },

                { TerrID.Madagascar, new List<TerrID>
                    {
                        TerrID.EastAfrica,
                        TerrID.SouthAfrica
                    }
                },

                { TerrID.Ural, new List<TerrID>
                    {
                        TerrID.Ukraine,
                        TerrID.Siberia,
                        TerrID.China,
                        TerrID.Afghanistan
                    }
                },

                { TerrID.Siberia, new List<TerrID>
                    {
                        TerrID.Ural,
                        TerrID.Yakutsk,
                        TerrID.Irkutsk,
                        TerrID.Mongolia,
                        TerrID.China
                    }
                },

                { TerrID.Yakutsk, new List<TerrID>
                    {
                        TerrID.Siberia,
                        TerrID.Kamchatka,
                        TerrID.Irkutsk
                    }
                },

                { TerrID.Kamchatka, new List<TerrID>
                    {
                        TerrID.Yakutsk,
                        TerrID.Alaska,
                        TerrID.Irkutsk,
                        TerrID.Japan,
                        TerrID.Mongolia
                    }
                },

                { TerrID.Irkutsk, new List<TerrID>
                    {
                        TerrID.Siberia,
                        TerrID.Yakutsk,
                        TerrID.Kamchatka,
                        TerrID.Mongolia
                    }
                },

                { TerrID.Afghanistan, new List<TerrID>
                    {
                        TerrID.Ukraine,
                        TerrID.Ural,
                        TerrID.China,
                        TerrID.India,
                        TerrID.MiddleEast
                    }
                },

                { TerrID.China, new List<TerrID>
                    {
                        TerrID.Ural,
                        TerrID.Siberia,
                        TerrID.Mongolia,
                        TerrID.Siam,
                        TerrID.India,
                        TerrID.Afghanistan
                    }
                },

                { TerrID.Mongolia, new List<TerrID>
                    {
                        TerrID.Siberia,
                        TerrID.Irkutsk,
                        TerrID.Kamchatka,
                        TerrID.Japan,
                        TerrID.China
                    }
                },

                { TerrID.Japan, new List<TerrID>
                    {
                        TerrID.Kamchatka,
                        TerrID.Mongolia
                    }
                },

                { TerrID.MiddleEast, new List<TerrID>
                    {
                        TerrID.SouthernEurope,
                        TerrID.Ukraine,
                        TerrID.Afghanistan,
                        TerrID.India,
                        TerrID.EastAfrica,
                        TerrID.Egypt
                    }
                },

                { TerrID.India, new List<TerrID>
                    {
                        TerrID.Afghanistan,
                        TerrID.China,
                        TerrID.Siam,
                        TerrID.MiddleEast
                    }
                },

                { TerrID.Siam, new List<TerrID>
                    {
                        TerrID.India,
                        TerrID.China,
                        TerrID.Indonesia
                    }
                },

                { TerrID.Indonesia, new List<TerrID>
                    {
                        TerrID.Siam,
                        TerrID.NewGuinea,
                        TerrID.WesternAustralia
                    }
                },

                { TerrID.NewGuinea, new List<TerrID>
                    {
                        TerrID.Indonesia,
                        TerrID.EasternAustralia,
                        TerrID.WesternAustralia
                    }
                },

                { TerrID.WesternAustralia, new List<TerrID>
                    {
                        TerrID.Indonesia,
                        TerrID.NewGuinea,
                        TerrID.EasternAustralia
                    }
                },

                { TerrID.EasternAustralia, new List<TerrID>
                    {
                        TerrID.NewGuinea,
                        TerrID.WesternAustralia
                    }
                },
        });
        /// <summary>
        /// Maps territories to their parent continents.
        /// </summary>
        /// <remarks>
        /// An early mapping taking advantage of the arbitrary order that happened to be used when building <see cref="TerrID"/> and <see cref="ContID"/>, <br/>
        /// this switch-expression based map is a touch more performant than the obvious <see cref="Dictionary{TKey, TValue}"/> alternative, but is only <br/>
        /// really maintainable if it's not to be extended or modified much at all. Under current assumptions, its fine. If those change, this should too.
        /// </remarks>
        /// <param name="terrID">The <see cref="TerrID"/> of the territory whose parent <see cref="ContID"/> is needed.</param>
        /// <returns>The <see cref="ContID"/> of the continent containing the territory.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="terrID"/> falls below -1 or above 41 (the min and max of <see cref="TerrID"/>.</exception>
        public static ContID TerrIDToContinent(TerrID terrID)
        {
            return (int)terrID switch {
                -1 => ContID.Null,
                int n when n >= 0 && n <= 8 => ContID.NorthAmerica,
                int n when n >= 9 && n <= 12 => ContID.SouthAmerica,
                int n when n >= 13 && n <= 19 => ContID.Europe,
                int n when n >= 20 && n <= 25 => ContID.Africa,
                int n when n >= 26 && n <= 37 => ContID.Asia,
                int n when n >= 38 && n <= 41 => ContID.Oceania,
                _ => throw new ArgumentOutOfRangeException(nameof(terrID))
            };
        }
        /// <summary>
        /// Determines whether a given continent is comprised of some set or subset of territories -- <br/>
        /// in other words, whether the "area" of the territories entirely "covers" the continent.
        /// </summary>
        /// <param name="territoryList">A <see cref="List{T}"/> of <see cref="TerrID"/> representing the territories to test.</param>
        /// <param name="continent">The <see cref="ContID"/> of the continet whose area may be included in that of the territories in <paramref name="territoryList"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="territoryList"/> includes *all* of the territories within <paramref name="continent"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IncludesContinent(List<TerrID> territoryList, ContID continent)
        {
            List<TerrID> continentList = ContinentMembers[continent];

            return continentList.All(territoryList.Contains);
        }
    }
    /// <summary>
    /// Builds an Earth board using configuration '.json' values.
    /// </summary>
    /// <param name="config">An <see cref="IConfiguration"/>. Typically injected by the DI system.</param>
    public EarthBoard(IConfiguration config, ILogger<EarthBoard> logger)
    {
        _logger = logger;
        Armies = [];
        TerritoryOwner = [];
        for (int i = 0; i < Geography.NumTerritories; i++) {
            Armies.Add((TerrID)i, int.Parse(config["startingArmies"] ?? "0"));
            TerritoryOwner.Add((TerrID)i, -1);
        }

        ContinentOwner = [];
        for (int i = 0; i < Geography.NumContinents; i++)
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
    /// Gets this board's geography.
    /// </summary>
    public IGeography Geography { get; } = new EarthGeography();
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
        var changedHomeContinent = EarthGeography.TerrIDToContinent(changed);
        List<TerrID> continentTerritories = EarthGeography.ContinentMembers[changedHomeContinent];
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

