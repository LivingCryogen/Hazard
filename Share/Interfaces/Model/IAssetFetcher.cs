namespace Share.Interfaces.Model;
/// <summary>
/// Exposes the Data Access 'Layer' to the Model, providing bespoke methods for retrieving Game Assets at run-time.
/// </summary>
/// <remarks>Expansion of the DAL to cover other Types requires extension of this interface.</remarks>
public interface IAssetFetcher
{
    /// <summary>
    /// Begins the process of data reading, conversion, and object initialization for <see cref="ICard"/>s. 
    /// </summary>
    /// <returns>An array of ready <see cref="ICard"/> instances for use by a <see cref="Model.Entities.Deck"/>, if data retrieval is successful. If not, <c>null</c>.</returns>
    List<ICardSet>? FetchCardSets();
    IRuleValues FetchRuleValues();
}
