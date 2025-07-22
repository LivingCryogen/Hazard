using Microsoft.Extensions.Logging;
using Model.Entities;
using Model.Entities.Cards;
using Shared.Geography;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Registry;

namespace Model.DataAccess;

/// <inheritdoc cref="IAssetFactory"/>
/// Currently only <see cref="TroopCard"/> is loaded from data files. To change this,
/// by adding, for example, another <see cref="$1ICard{T}$2"/>, this class must extend. */
public class AssetFactory : IAssetFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AssetFactory> _logger;
    private readonly IDataProvider? _dataProvider;

    /// <summary>
    /// Constructs an Asset Factory without a Data Provider.
    /// </summary>
    /// <param name="logger">A logger for logging debug information and errors.</param>
    /// <param name="loggerFactory">Instantiates <see cref="ILogger"/>s.</param>
    public AssetFactory(ILogger<AssetFactory> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }
    /// <summary>
    /// Constructs an Asset Factory with an injected Data Provider.
    /// </summary>
    /// <param name="logger">A logger for logging debug information and errors.</param>
    /// <param name="dataProvider">Provides data from external sources (e.g., data files).</param>
    /// <param name="loggerFactory">Instantiates <see cref="ILogger"/>s.</param>
    public AssetFactory(IDataProvider dataProvider, ILogger<AssetFactory> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _dataProvider = dataProvider;
    }

    /// <summary>
    /// Builds instances of <see cref="Type"/>s registered in a <see cref="ITypeRegister{T}"/>, from data provided by a <see cref="IDataProvider"/>.
    /// </summary>
    /// <remarks>
    /// <example>For example, the file "TroopCardSet.json" is handled by a <see cref="IDataProvider"/>.<br/>
    /// The provider uses objects and relations registered in a <see cref="ITypeRegister{T}"/>.<br/>
    /// <see cref="AssetFactory"/> calls <see cref="IDataProvider.GetData(string)"/> and builds concrete instances of game assets out of its return value.</example>
    /// </remarks>
    /// <param name="typeName">The string marked by <see cref="RegistryRelation.Name"/> as a name for a keyed <see cref="Type"/> in an <see cref="ITypeRegister{T}"/>.<br/>
    /// The entry must also contain an <see cref="object"/> marked <see cref="RegistryRelation.DataConverter"/>, and possibly <see cref="RegistryRelation.ConvertedDataType"/>.
    /// </param>
    /// <returns>The constructed instance.</returns>
    public object? GetAsset(string typeName)
    {
        var dataObject = _dataProvider?.GetData(typeName);

        switch (dataObject)
        {
            case ICardSet<TerrID> cardSet:
                cardSet.Cards = [.. BuildTroopCards(cardSet)];
                return cardSet;
            case GeographyInitializer geographyInitializer:
                return geographyInitializer;
            default: return null;
        }
    }
    /// <summary>
    /// Builds TroopCards from the data in <see cref="TroopCardSetData"/>.
    /// </summary>
    /// <param name="troopCardSet">The <see cref="RegistryRelation.ConvertedDataType"/> of a <see cref="Cards.TroopCardSetDataJConverter"/>.
    /// within a <see cref="TypeRegister"/> entry for <see cref="TroopCard"/>.<br/> An instance is returned by <see cref="IDataProvider.GetData(string)"/> when passsed the <see cref="object"/> marked <see cref="RegistryRelation.Name"/>
    /// <br/>for <see cref="TroopCard"/> if the entry also includes a proper <see cref="RegistryRelation.DataFileName"/>.</param>
    /// <returns>An array of TroopCards for use by <see cref="Deck"/>.</returns>
    public TroopCard[] BuildTroopCards(ICardSet<TerrID> troopCardSet)
    {
        List<TroopCard> troopCards = [];
        if (troopCardSet.JData == null ||
            troopCardSet.JData.Targets == null ||
            ((ITroopCardSetData<TerrID>)troopCardSet.JData).Insignia == null)
        {
            _logger.LogWarning($"Valid ICardSetData for TroopCards not found by AssetFactory.");
            return [];
        }

        int numTroopCards = troopCardSet.JData.Targets.Length;
        for (int i = 0; i < numTroopCards; i++)
        {
            List<TerrID> targets = [];
            for (int j = 0; j < troopCardSet.JData.Targets[i].Length; j++)
                targets.Add(troopCardSet.JData.Targets[i][j]);

            troopCards.Add(new TroopCard(_loggerFactory.CreateLogger<TroopCard>())
            {
                CardSet = troopCardSet,
                Target = [.. targets],
                Insigne = ((ITroopCardSetData<TerrID>)troopCardSet.JData).Insignia[i],
            });
        }

        if (troopCards.Count <= 0)
        {
            _logger.LogWarning($"TroopCardSet factory returned null set.");
            return [];
        }

        return [.. troopCards];
    }
}
