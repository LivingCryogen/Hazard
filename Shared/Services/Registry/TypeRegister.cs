namespace Shared.Services.Registry;

/// <inheritdoc cref="ITypeRegister{T}"/>
public class TypeRegister : ITypeRegister<ITypeRelations>
{
    private readonly Dictionary<Type, ITypeRelations> _typeRelata = [];

    /// <summary>
    /// Constructs the registry and populates it with default entries.
    /// </summary>
    /// <param name="initializer">An implementation of <see cref="IRegistryInitializer"/>, such as <see cref="RegistryInitializer"/>, which provides default values for initial Registry entries.</param>
    public TypeRegister(IRegistryInitializer initializer)
    {
        initializer.PopulateRegistry(this);
    }

    /** <inheritdoc cref="ITypeRegister{T}.this[string]"/>
     * Implementation: Performs a linear search through the dictionary for values containing <paramref name="registeredName"/>.
     * If <see cref="_typeRelata"/> remains small, this is fine. If it were to grow large, it would be better to split off Names
     * from <see cref="ITypeRelations"/> and create a dedicated Name/Type dictionary. */
    public Type? this[string registeredName] {
        get {
            if (string.IsNullOrEmpty(registeredName))
                return null;

            foreach (Type type in _typeRelata.Keys) {
                if (_typeRelata[type][RegistryRelation.Name] != null) {
                    if ((string?)_typeRelata[type][RegistryRelation.Name] == registeredName)
                        return type;
                }
            }

            return null;
        }
    }
    /** <inheritdoc cref="ITypeRegister{T}.this[Type]"/>
     * Getter catches <see cref="KeyNotFoundException"/>, returning null instead.*/
    public ITypeRelations? this[Type type] {
        get {
            try {
                return _typeRelata[type];
            } catch (KeyNotFoundException e) { _ = e; return null; }
        }
        set {
            if (value is ITypeRelations and not null)
                Register(type, value);
            else throw new ArgumentException($"{value} is not a valid instance as it does not implement ITypeRelations.", nameof(value));
        }
    }
    /** <inheritdoc cref="ITypeRegister{T}.this[RegistryRelation]"/>
     * Performs a linear search through <see cref="_typeRelata"/> */
    public (Type KeyType, object RelatedObject)[]? this[RegistryRelation relation] {
        get {
            List<(Type, object)> entries = [];
            foreach (Type type in _typeRelata.Keys) {
                if (_typeRelata[type][relation] != null) {
                    entries.Add(new(type, _typeRelata[type][relation]!));
                }
            }
            if (entries.Count > 0)
                return [.. entries];
            else return null;
        }
    }
    /** <inheritdoc cref="ITypeRegister{T}.Register(Type)"/>
     * This implementation's default relies on Reflection to set a minimum entry value of Type.Name. <br/>
     * For the expected scope of this application, this should not be a performance concern.
     * <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is <c>null</c>.</exception>
     * <exception cref="ArgumentException">Thrown if <paramref name="type"/> is already registered.</exception> */
    public void Register(Type type)
    {
        if (_typeRelata.ContainsKey(type))
            throw new ArgumentException($"{type} is already a registered Type. If you want to edit the Relations of a registered Type, use AddRelation or RemoveRelation methods.");

        _typeRelata.Add(type, new TypeRelations([(type.Name, RegistryRelation.Name)]));
    }
    /** <inheritdoc cref="ITypeRegister{T}.Register(Type, T)"/>
     * <exception cref="ArgumentNullException">Thrown if either parameter, <paramref name="type"/> or <paramref name="typeRelations"/>, is <c>null</c>.</exception>
     * <exception cref="ArgumentException">Thrown if <paramref name="type"/> is already registered.</exception> */
    public void Register(Type type, ITypeRelations typeRelations)
    {
        if (_typeRelata.ContainsKey(type))
            throw new ArgumentException($"{type} is already a registered Type. If you want to edit the Relations of a registered Type, use AddRelation or RemoveRelation methods.");

        _typeRelata.Add(type, typeRelations);
    }

    /// <exception cref="ArgumentNullException">The registered <see cref="Type"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="type"/> is already registered.</exception>
    public void AddRelation(Type type, (object, RegistryRelation) newRelation)
    {
        if (_typeRelata.TryGetValue(type, out ITypeRelations? value))
            value.Add(newRelation.Item1, newRelation.Item2);
        else
            throw new ArgumentException($"{type} is not a registered Type. If you want to add a Type to the Registry, use Type parameter indexer or the Register method.");
    }
    /// <summary>
    /// Remove an object relation from a pre-existing registry entry. If the resulting entry has an empty set of object relations, it is entirely removed via <seealso cref="DeRegister(Type)"/>.
    /// </summary>
    /// <param name="type">The registered <see cref="Type"/></param>
    /// <param name="targetRelation">The <see cref="RegistryRelation"/> to remove, along with its paired <see cref="object"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is <c>null</c>.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if <paramref name="type"/> is not found in the registry.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="targetRelation"/> is not registered to <paramref name="type"/>.</exception>
    public void RemoveRelation(Type type, RegistryRelation targetRelation)
    {
        if (_typeRelata.TryGetValue(type, out ITypeRelations? relations)) {
            if (relations[targetRelation] == null)
                throw new ArgumentException($"{targetRelation} was not found in the entry for {type}");
            relations.Remove(targetRelation);
            if (relations.IsEmpty)
                DeRegister(type);
        }
        else throw new KeyNotFoundException($"{type} is not a registered Type. If you want to add a Type to the Registry, use Type parameter indexer or the Register method.");
    }
    /// <summary>
    /// Remove a Type's entry, including any contained object relations.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to remove.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="type"/> is not found in the registry.</exception>
    public void DeRegister(Type type)
    {
        if (!_typeRelata.ContainsKey(type))
            throw new ArgumentException($"{type} is not a registered Type.");

        _typeRelata.Remove(type);
    }
    /// <summary>
    /// If the registry has entries, delete them.
    /// </summary>
    public void Clear()
    {
        if (_typeRelata?.Count > 0)
            _typeRelata.Clear();
    }
    /// <inheritdoc cref="ITypeRegister{T}.TryGetParentType(Type, out Type?)"/>
    public bool TryGetParentType(Type registeredType, out Type? parentType)
    {
        if (!_typeRelata.TryGetValue(registeredType, out ITypeRelations? registeredRelations) || registeredRelations == null) {
            parentType = null;
            return false;
        }
        if (registeredRelations[RegistryRelation.CollectionType] is not Type collection) {
            parentType = null;
            return false;
        }
        // Validate parent-member relationship
        if (registeredRelations[RegistryRelation.CollectionName] is not string regCollectionName || regCollectionName != collection.Name)
            throw new InvalidDataException($"The name of the type registered as the parent collection of type {registeredType} did not match the registered collection name of {registeredType}.");
        if (!_typeRelata.TryGetValue(collection, out ITypeRelations? regCollectionRelations) || regCollectionRelations == null)
            throw new InvalidDataException($"The type registered as the parent collection of type {registeredType} did not have any registered relations with which to validate its relation to {registeredType}.");
        if (regCollectionRelations[RegistryRelation.ElementType] is not Type regMemberType || regMemberType != registeredType)
            throw new InvalidDataException($"The type registered as the parent collection of type {registeredType} did not have a registered member type equal to type {registeredType}.");

        parentType = collection;
        return true;
    }
}
