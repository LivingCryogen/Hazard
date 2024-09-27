using Hazard_Model.Entities;
using Hazard_Model.Entities.Cards;
using Hazard_Share.Enums;
using Hazard_Share.Interfaces.Model;
using Hazard_Share.Services.Registry;
using Microsoft.Extensions.Logging;

namespace Hazard_Model.DataAccess;

/** <inheritdoc cref="IAssetFactory"/>
 * Currently only <see cref="TroopCard"/> is loaded from data files. To change this,
 * by adding, for example, another <see cref="ICard"/>, this class must extend. */
public class AssetFactory : IAssetFactory
{
    private readonly ILogger _logger;
    private readonly IDataProvider? _dataProvider;

    /// <summary>
    /// Constructs an <see cref="AssetFactory"/> with an injected <see cref="ILogger"/> but without a <see cref="IDataProvider"/>.
    /// </summary>
    /// <param name="logger">An <see cref="ILogger"/> for logging debug information and errors.</param>
    public AssetFactory(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Constructs an <see cref="AssetFactory"/> instance with injected logger and <see cref="IDataProvider"/>.
    /// </summary>
    /// <param name="logger">An <see cref="ILogger"/> for logging debug information and errors.</param>
    /// <param name="dataProvider">An <see cref="IDataProvider"/> which reads off data from external sources (e.g., data files).</param>
    public AssetFactory(ILogger<AssetFactory> logger, IDataProvider dataProvider)
    {
        _logger = logger;
        _dataProvider = dataProvider;
    }
    /** <summary>
     * Builds instances of <see cref="Type"/>s registered in a <see cref="Hazard_Share.Services.Registry.ITypeRegister{T}"/>, from data provided by a <see cref="IDataProvider"/>.
     * </summary>   
     * <remarks>
     * <example>E.g.: The file "TroopCardSet.json" is handled by a <see cref="IDataProvider"/> using objects
     * set out in an <see cref="ITypeRegister{T}"/>. <see cref="AssetFactory"/> calls <see cref="IDataProvider.GetData(string)"/> and
     * builds concrete instances of game assets out of its return value.</example></remarks>
     * <param name="typeName">
     * The string marked by <see cref="RegistryRelation.Name"/> as a name for a keyed 
     * <see cref="Type"/> in an <see cref="ITypeRegister{T}"/>.<br/>
     * The entry must also have an <see cref="object"/> marked
     * <see cref="RegistryRelation.DataConverter"/>, and possibly <see cref="RegistryRelation.ConvertedDataType"/>.
     * </param> */
    public object? GetAsset(string typeName)
    {
        var dataObject = _dataProvider?.GetData(typeName);

        if (dataObject is ICardSet cardSet) {
            cardSet.Cards = BuildTroopCards(cardSet);
            return cardSet;
        }

        return null;
    }
    /// <summary>
    /// Builds an array of <see cref="TroopCard"/> from the data in <see cref="TroopCardSetData"/>.
    /// </summary>
    /// <param name="troopCardSet">The <see cref="RegistryRelation.ConvertedDataType"/> of a <see cref="Cards.TroopCardSetDataJConverter"/>.
    /// within a <see cref="TypeRegister"/> entry for <see cref="TroopCard"/>.<br/> An instance is returned by <see cref="IDataProvider.GetData(string)"/> when passsed the <see cref="object"/> marked <see cref="RegistryRelation.Name"/>
    /// <br/>for <see cref="TroopCard"/> if the entry also includes a proper <see cref="RegistryRelation.DataFileName"/>.</param>
    /// <returns>An <see langword="array"/> of <see cref="TroopCard"/> for use by <see cref="Deck"/>.</returns>
    public TroopCard[]? BuildTroopCards(ICardSet troopCardSet)
    {
        List<TroopCard> troopCards = [];
        if (troopCardSet.JData == null || troopCardSet.JData.Targets == null || ((ITroopCardSetData)troopCardSet.JData).Insignia == null) {
            _logger.LogWarning($"Valid ICardSetData for TroopCards not found by AssetFactory.");
            return null;
        }
        int numTroopCards = troopCardSet.JData.Targets.Length;
        for (int i = 0; i < numTroopCards; i++) {
            List<TerrID> targets = [];
            for (int j = 0; j < troopCardSet.JData.Targets[i].Length; j++)
                targets.Add(troopCardSet.JData.Targets[i][j]);

            troopCards.Add(new TroopCard(troopCardSet, ) {
                Target = [.. targets],
                Insigne = ((ITroopCardSetData)troopCardSet.JData).Insignia[i],
            });
        }

        if (troopCards.Count > 0)
            return [.. troopCards];
        else {
            _logger.LogWarning($"TroopCardSet factory returned null set.");
            return null;
        }
    }
}
