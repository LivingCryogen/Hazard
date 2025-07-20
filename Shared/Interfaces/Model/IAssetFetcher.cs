using Shared.Geography;

namespace Shared.Interfaces.Model;
/// <summary>
/// Exposes the Data Access 'Layer' to the Model, providing bespoke methods for retrieving Game Assets at run-time.
/// </summary>
/// <remarks>Expansion of the DAL to cover other Types requires extension of this interface.</remarks>
public interface IAssetFetcher<T> where T: struct, Enum
{
    /// <summary>
    /// Begins the process of data reading, conversion, and object initialization for <see cref="ICard"/>s. 
    /// </summary>
    /// <returns>A list of card sets whose <see cref="ICardSet.Cards"/>.</returns>
    List<ICardSet<T>> FetchCardSets();
    /// <summary>
    /// Begins the process of data reading, conversion, and object initialization for <see cref="BoardGeography"/>.
    /// </summary>
    /// <returns>An initializer object to be used with <see cref="BoardGeography.Initialize"/>.</returns>
    GeographyInitializer FetchGeography();
}
