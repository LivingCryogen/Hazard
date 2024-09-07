namespace Hazard_Share.Services.Registry;

/// <summary>
/// Registers <see cref="Type"/>s with <see cref="object"/>s by <see cref="RegistryRelation"/>, enabling look-up at runtime. 
/// <para><example>
/// E.g. the <see cref="Type"/> : <c>typeof(TroopCard)</c>, if registered with <c>"TroopCard"</c>,
/// can be retreived by:<code>Type troopCardType = Registry["TroopCard"];</code> where <c>Registry</c> is an instance of <c>TypeRegister</c>.</example></para>   
/// </summary>
/// <typeparam name="T">The <see cref="Type"/> of the <see cref="ITypeRelations"/> implementation which will wrap the values for a keyed <see cref="Type"/> within a registry entry.</typeparam>
public interface ITypeRegister<T> where T : ITypeRelations
{
    /// <summary>
    /// Type look-up by name.
    /// </summary>
    /// <param name="registeredName">The name registered by <see cref="RegistryRelation.Name"/></param>
    /// <returns>A <see cref="Type"/> instance if one is registered by <paramref name="registeredName"/>; if not, <c>null</c>.</returns>
    Type? this[string registeredName] { get; }
    /// <summary>
    /// Registry entry look-up or set-up by Type.
    /// </summary>
    /// <param name="type">The registered Type.</param>
    /// <returns>An instance implementing <see cref="ITypeRelations"/>, with a list of entries (objects under a specific <see cref="RegistryRelation"/>). Or <c>null</c> if <paramref name="type"/> is unregistered</returns>
    /// <exception cref="ArgumentNullException">Thrown when setting an entry with a <c>null</c> value.</exception>
    /// <exception cref="ArgumentException">Thrown when setting an entry with a value that does not implement <see cref="ITypeRelations"/>.</exception>
    T? this[Type type] { get; set; }
    /// <summary>
    /// Get all objects registered under a specific <see cref="RegistryRelation"/>.
    /// </summary>
    /// <param name="relation">The target <see cref="RegistryRelation"/>.</param>
    /// <returns>An array of Tuples - (<see cref="Type"/>, <see cref="object"/>)[] - containing each Key/Object related by <paramref name="relation"/>; or, if none are found, <c>null</c>.</returns>
    (Type KeyType, object RelatedObject)[]? this[RegistryRelation relation] { get; }
    /// <summary>
    /// Register a Type. 
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to register.</param>
    /// <remarks>This method is intended for use with a default registry entry form, such <br/>
    /// that the <see cref="object"/> is automatically provided for a common <see cref="RegistryRelation"/>,<br/>
    /// e.g.: <see cref="Type.FullName"/> or <see langword="nameof"/>(<see cref="Type"/>) for <see cref="RegistryRelation.Name"/>.</remarks>
    void Register(Type type);
    /// <summary>
    /// Register a Type with object relations provided by an <see cref="ITypeRelations"/> instance.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to register.</param>
    /// <param name="typeRelations">The <see cref="ITypeRelations"/> implementation containing object relations for the entry.</param>
    void Register(Type type, T typeRelations);
    /// <summary>
    /// Add an object relation to a pre-existing registry entry.
    /// </summary>
    /// <param name="type">The registered <see cref="Type"/>.</param>
    /// <param name="newRelation">A Tuple pair containing the object relation, in the form <c>(<see cref="object"/>, <see cref="RegistryRelation"/>)</c>.</param>
    void AddRelation(Type type, (object, RegistryRelation) newRelation);
    /// <summary>
    /// Removes an object relation from a pre-existing registry entry.
    /// </summary>
    /// <param name="type">The registered <see cref="Type"/>.</param>
    /// <param name="targetRelation">The <see cref="RegistryRelation"/> to remove.</param>
    void RemoveRelation(Type type, RegistryRelation targetRelation);
    /// <summary>
    /// Removes a registry entry.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to be deregistered.</param>
    void DeRegister(Type type);
    /// <summary>
    /// Empties the registry.
    /// </summary>
    void Clear();
}