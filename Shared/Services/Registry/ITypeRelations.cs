namespace Shared.Services.Registry;

/// <summary>
/// Encapsulates objects under specific relations as values for a <see cref="TypeRegister"/> entry.
/// </summary>
public interface ITypeRelations
{
    /// <summary>
    /// Indicates whether the instance is internally empty.
    /// </summary>
    /// <value>
    /// True if the set of object-relation values is empty; otherwise, false.
    /// </value>
    bool IsEmpty { get; }
    /// <summary>
    /// Retrieves the <see cref="object"/> in a <see cref="TypeRegister"/> entry given its relation type.
    /// </summary>
    /// <param name="relation">The relation under which the <see cref="object"/> was registered.</param>
    /// <returns>The related <see cref="object"/> or, if none, <see langword="null"/>.</returns>
    object? this[RegistryRelation relation] { get; }
    /// <summary>
    /// Adds an object under a specific relation.
    /// </summary>
    /// <param name="obj">The <see cref="object"/> to be registered as related.</param>
    /// <param name="relation">The relation type.</param>
    void Add(object obj, RegistryRelation relation);
    /// <summary>
    /// Removes an object/relation pair.
    /// </summary>
    /// <param name="relation">The relation with which the value was registered.</param>
    void Remove(RegistryRelation relation);
}