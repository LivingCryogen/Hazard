namespace Shared.Services.Registry;

/// <summary>
/// Contains values for the keyed <see cref="Type"/> in a <see cref="TypeRegister"/> entry.
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
    /// <param name="relation">The <see cref="RegistryRelation"/> with which the <see cref="object"/> was registered.</param>
    /// <returns>The related <see cref="object"/> or, if none, <c>null</c>.</returns>
    object? this[RegistryRelation relation] { get; }

    /// <summary>
    /// Adds another element to the pre-existing <see cref="TypeRelations"/> value for a keyed <see cref="Type"/> within a <see cref="TypeRegister"/>.
    /// </summary>
    /// <param name="obj">The <see cref="object"/> to be related to the <see cref="Type"/>.</param>
    /// <param name="relation">The <see cref="RegistryRelation"/> describing the <paramref name="obj"/>'s relation to the <see cref="Type"/>.</param>
    void Add(object obj, RegistryRelation relation);
    /// <summary>
    /// Removes a value from a pre-existing keyed <see cref="Type"/> within a <see cref="TypeRegister"/>.
    /// </summary>
    /// <param name="relation">The <see cref="RegistryRelation"/> with which the value was registered.</param>
    void Remove(RegistryRelation relation);
}