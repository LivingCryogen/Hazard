namespace Share.Services.Registry;

/** <inheritdoc cref="ITypeRelations"/>
 * Values are restricted to a single object per relation; this may or may not need to change in the future,
 * depending on how easy it is to keep <see cref="RegistryRelation"/> reasonably short and tidy. */
public class TypeRelations : ITypeRelations
{
    private readonly Dictionary<RegistryRelation, object> _relatedObjects = [];

    /// <summary>
    /// Constructs an empty <see cref="TypeRelations"/> instance.
    /// </summary>
    public TypeRelations()
    { }
    /// <summary>
    /// Constructs a <see cref="TypeRelations"/> instance from an array of objects and their relation to a keyed <see cref="Type"/> within a <see cref="TypeRegister"/>. 
    /// </summary>
    /// <param name="relations">An array of Tuples containing the objects related to the keyed <see cref="Type"/>, each with a <see cref="RegistryRelation"/> categorizing that relation.</param>
    public TypeRelations((object, RegistryRelation)[] relations)
    {
        foreach (var relation in relations)
            Add(relation.Item1, relation.Item2);
    }

    /// <inheritdoc cref="ITypeRelations.IsEmpty"/>
    public bool IsEmpty { get => _relatedObjects.Count == 0; }
    /// <inheritdoc cref="ITypeRelations.this[RegistryRelation]"/>
    public object? this[RegistryRelation relation] {
        get {
            if (_relatedObjects.TryGetValue(relation, out object? value))
                return value;

            return null;
        }
    }

    /** <inheritdoc cref="ITypeRelations.Add(object, RegistryRelation)"/>
     * <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is <c>null</c>.</exception>
     * <exception cref="ArgumentException">Thrown if there is already an <see cref="object"/> associated with the given <paramref name="relation"/>, or if the type of <paramref name="obj"/> is incompatible with the given <paramref name="relation"/>.</exception>
     * Extension of <see cref="RegistryRelation"/> will usually require adding additional handling here. */
    public void Add(object obj, RegistryRelation relation)
    {
        if (_relatedObjects.ContainsKey(relation))
            throw new ArgumentException("This object has already been related (A given object can be related to a Type only once).", nameof(obj));
        switch (relation) {
            case RegistryRelation.Name:
                if (obj is not string)
                    throw new ArgumentException($"{obj} is not a string. {relation} only targets objects of type string.");
                else
                    _relatedObjects.Add(relation, obj);
                break;
            case RegistryRelation.CollectionName:
                if (obj is not string)
                    throw new ArgumentException($"{obj} is not a string. {relation} only targets objects of type string.");
                else
                    _relatedObjects.Add(relation, obj);
                break;
            case RegistryRelation.CollectionType:
                if (obj is not Type)
                    throw new ArgumentException($"{obj} is not a Type. {relation} only targets Type objects.");
                _relatedObjects.Add(relation, obj);
                break;
            case RegistryRelation.ElementType:
                if (obj is not Type)
                    throw new ArgumentException($"{obj} is not a Type. {relation} only targets Type objects.");

                _relatedObjects.Add(relation, obj);
                break;
            case RegistryRelation.DataFileName:
                if (obj is not string)
                    throw new ArgumentException($"{obj} is not a string. {relation} only targets objects of type string.");
                else
                    _relatedObjects.Add(relation, obj);
                break;
            case RegistryRelation.DataConverter:
                if (!obj.GetType().IsClass)
                    throw new ArgumentException($"{obj} is not a reference object, and cannot be a DataConverter.");
                else
                    _relatedObjects.Add(relation, obj);
                break;
            case RegistryRelation.ConvertedDataType:
                if (obj is Type)
                    _relatedObjects.Add(relation, obj);
                else
                    throw new ArgumentException($"{obj} is not a Type, and cannot be registered as a ConvertedDataType.");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(relation), $"{relation} was not accepted. Verify that this ITypeRelations instance accepts {relation}.");
        }
    }
    /** <inheritdoc cref="ITypeRelations.Remove(RegistryRelation)"/>
     * <exception cref="KeyNotFoundException">Thrown if <paramref name="relation"/> was not found.</exception> */
    public void Remove(RegistryRelation relation)
    {
        if (!_relatedObjects.ContainsKey(relation))
            throw new KeyNotFoundException(nameof(relation));

        _relatedObjects.Remove(relation);

        if (_relatedObjects.Values.Count == 0)
            _relatedObjects.Clear();
    }
}
