namespace Share.Services.Registry;

/**<summary>
 * Qualifies the relation between an <see cref="object"/> and a keyed <see cref="Type"/> in a <see cref="ITypeRegister{T}"/> registry.
 * </summary>
 * Subject to expansion. For now the value class for a <see cref="ITypeRegister{T}"/>, <see cref="ITypeRelations"/>, is restricted 
 * to a single <see cref="object"/> per <see cref="RegistryRelation"/>. If this expands beyond, say, a dozen categories, it may 
 * be time to change that. */
public enum RegistryRelation
{
    /// <summary>
    /// Marks an <see cref="object"/> as a name for a keyed <see cref="Type"/> in a <see cref="TypeRegister"/>.
    /// </summary>
    Name,
    /// <summary>
    /// Marks an <see cref="object"/> as the name of a <see cref="RegistryRelation.CollectionType"/> associated with a keyed <see cref="Type"/> in a <see cref="TypeRegister"/>.
    /// </summary>
    CollectionName,
    /// <summary>
    /// Marks an <see cref="object"/> as a collection <see cref="Type"/> for the keyed <see cref="Type"/> in a <see cref="TypeRegister"/>.
    /// </summary>
    CollectionType,
    /// <summary>
    /// Marks an <see cref="object"/> as an element <see cref="Type"/> contained within a collection (<see cref="RegistryRelation.CollectionName"/> and <br/>
    /// <see cref="RegistryRelation.CollectionType"/>) which is registered in a <see cref="TypeRegister"/>.
    /// </summary>
    ElementType,
    /// <summary>
    /// Marks an <see cref="object"/> as the name of a data file for a keyed <see cref="Type"/> in a <see cref="TypeRegister"/>.
    /// </summary>
    DataFileName,
    /// <summary>
    /// Marks an <see cref="object"/> as a data converter for a keyed <see cref="Type"/> in a <see cref="TypeRegister"/>.
    /// </summary>
    DataConverter,
    /// <summary>
    /// Marks an <see cref="object"/> as the target conversion <see cref="Type"/> of a data converter for a keyed <see cref="Type"/> in a <see cref="TypeRegister"/>.
    /// </summary>
    ConvertedDataType
}
