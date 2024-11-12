namespace Shared.Services.Registry;

/// <summary>
/// Registers Types with objects by <see cref="RegistryRelation"/>, enabling look-up at runtime. 
/// <para><example>
/// E.g. the <see cref="Type"/> : <c>typeof(<see cref="Model.Entities.Cards.TroopCard"/>)</c>, if registered with <c>"TroopCard"</c>,
/// can be retreived by:<code>Type troopCardType = Registry["TroopCard"];</code> where <c>Registry</c> is an instance of <c>TypeRegister</c>.</example></para>   
/// </summary>
/// <typeparam name="T">Encapsulates related values for a registry entry.</typeparam>
public interface ITypeRegister<T> where T : ITypeRelations
{
    /// <summary>
    /// Type look-up by name.
    /// </summary>
    /// <param name="registeredName">The name registered under <see cref="RegistryRelation.Name"/></param>
    /// <returns>The type registered under <paramref name="registeredName"/>; if none, <see langword="null"/>.</returns>
    Type? this[string registeredName] { get; }
    /// <summary>
    /// Registry entry look-up or set-up by Type.
    /// </summary>
    /// <param name="type">The registered Type.</param>
    /// <returns>Related objects, or <see langword="null"/> if <paramref name="type"/> is unregistered.</returns>
    /// <exception cref="ArgumentNullException">Thrown when setting an entry with a null value.</exception>
    /// <exception cref="ArgumentException">Thrown when setting an entry with a value that does not implement <see cref="ITypeRelations"/>.</exception>
    T? this[Type type] { get; set; }
    /// <summary>
    /// Get all objects registered under a specific <see cref="RegistryRelation"/>.
    /// </summary>
    /// <param name="relation">The target <see cref="RegistryRelation"/>.</param>
    /// <returns>Each Key/Object related by <paramref name="relation"/>; or, if none are found, <see langword="null"/>.</returns>
    (Type KeyType, object RelatedObject)[]? this[RegistryRelation relation] { get; }
    /// <summary>
    /// Register a Type. 
    /// </summary>
    /// <param name="type">The type to register.</param>
    /// <remarks>
    /// This assumes a default registry entry, e.g.: <see cref="Type.FullName"/> or <see langword="nameof"/>(<see cref="Type"/>) for <see cref="RegistryRelation.Name"/>.
    /// </remarks>
    void Register(Type type);
    /// <summary>
    /// Register a Type with a given set of relations to objects.
    /// </summary>
    /// <param name="type">The type to register.</param>
    /// <param name="typeRelations">The relations to register to <paramref name="type"/>.</param>
    void Register(Type type, T typeRelations);
    /// <summary>
    /// Add an object relation to a pre-existing registry entry.
    /// </summary>
    /// <param name="type">The registered type.</param>
    /// <param name="newRelation">The related object paired with its relation type.</param>
    void AddRelation(Type type, (object, RegistryRelation) newRelation);
    /// <summary>
    /// Removes an object relation from a pre-existing registry entry.
    /// </summary>
    /// <param name="type">The registered type.</param>
    /// <param name="targetRelation">The relation to remove.</param>
    void RemoveRelation(Type type, RegistryRelation targetRelation);
    /// <summary>
    /// Removes a registry entry.
    /// </summary>
    /// <param name="type">The type to be deregistered.</param>
    void DeRegister(Type type);
    /// <summary>
    /// Empties the registry.
    /// </summary>
    void Clear();
    /// <summary>
    /// Checks whether a registered type has a registered parent, and returns it if so.
    /// </summary>
    /// <param name="registeredType">The type already registered.</param>
    /// <param name="parentType">The type registered as parent of <paramref name="registeredType"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="registeredType"/> has a registered parent type in the registry; otherwise, <see langword="false"/>.</returns>
    bool TryGetParentType(Type registeredType, out Type? parentType);
}