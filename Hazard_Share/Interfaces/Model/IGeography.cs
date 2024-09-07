using Hazard_Share.Enums;
using System.Collections.ObjectModel;

namespace Hazard_Share.Interfaces.Model;

/// <summary>
/// A data object containing constants and methods that represent the relationships between territories (<see cref="TerrID"/> and continents (<see cref="ContID"/> on an <see cref="IBoard"/>.
/// <remarks>
/// Currently contains hard-coded values; these should be replaced by '.json' data files, like was done for <see cref="ICard"/>s.
/// </remarks>
/// </summary>
public interface IGeography
{
    /// <summary>
    /// Gets the number of territories on the <see cref="IBoard"/>.
    /// </summary>
    /// <value>
    /// An integer equal to the number of values in <see cref="TerrID"/>.
    /// </value>
    public int NumTerritories { get; }
    /// <summary>
    /// Gets the number of continents on the <see cref="IBoard"/>
    /// </summary>
    /// <value>
    /// An integer equal to the number of values in <see cref="ContID"/>.
    /// </value>
    public int NumContinents { get; }

    /// <summary>
    /// Gets a list of territories included in a given continent.
    /// </summary>
    /// <value>
    /// A read only dictionary of <see cref="ContID"/> to <see cref="TerrID"/>s.
    /// </value>
    public static ReadOnlyDictionary<ContID, List<TerrID>>? ContinentMembers { get; }
    /// <summary>
    /// Gets a list of territories which neighbor a specified territory.
    /// </summary>
    /// <value>
    /// A read only dictionary of other <see cref="TerrID"/>s to each <see cref="TerrID"/>. This, as it turns out, is a manually implemented 'Graph',<br/>
    /// with each key representing a 'node' and each value the 'edges.' Changing to standard .NET implementation of the data structure may be beneficial.
    /// </value>
    public ReadOnlyDictionary<TerrID, List<TerrID>>? NeighborWeb { get; }
    /// <summary>
    /// Determines which continent a specified territory belongs to.
    /// </summary>
    /// <param name="terrID">The ID of the specified territory.</param>
    /// <returns>The ID of the containing continent.</returns>
    /// <exception cref="NotImplementedException"></exception>
    public virtual ContID TerrIDtoContID(TerrID terrID) { throw new NotImplementedException(); }
    /// <summary>
    /// Determines whether a given set of territories fully encompasses a specified continent.
    /// </summary>
    /// <param name="territoryList">The set of territories (in <see cref="TerrID"/> form).</param>
    /// <param name="continent">The continent (<see cref="ContID"/>) to test.</param>
    /// <returns><see langword="true"/> if every territory of the <paramref name="continent"/> is present in the <paramref name="territoryList"/>; otherwise, <see langword="false"/>.</returns>.
    /// <exception cref="NotImplementedException"></exception>
    public virtual bool IncludesContinent(List<TerrID> territoryList, ContID continent) { throw new NotImplementedException(); }
}

