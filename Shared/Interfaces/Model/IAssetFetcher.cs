using Shared.Geography;

namespace Shared.Interfaces.Model;
/// <summary>
/// Exposes the Data Access 'Layer' to the Model, providing bespoke methods for retrieving Game Assets at run-time.
/// </summary>
/// <remarks>Expansion of the DAL to cover other Types requires extension of this interface.</remarks>
public interface IAssetFetcher
{
    /// <summary>
    /// Begins the process of data reading, conversion, and object initialization for <see cref="ICard"/>s. 
    /// </summary>
    /// <returns>An array of ready <see cref="ICard"/> instances for use by a Deck, if data retrieval is successful. If not, <see langword="null"/>.</returns>
    List<ICardSet> FetchCardSets();
    /// <summary>
    /// Begins the process of data reading, conversion, and object initialization for <see cref="IRuleValues"/>.
    /// </summary>
    /// <returns>The built RuleValues object.</returns>
    IRuleValues FetchRuleValues();
    /// <summary>
    /// Begins the process of data reading, conversion, and object initialization for <see cref="BoardGeography"/>.
    /// </summary>
    /// <returns>An initializer object to be used with <see cref="BoardGeography.Initialize(GeographyInitializer)"/>.</returns>
    GeographyInitializer FetchGeography();
}
