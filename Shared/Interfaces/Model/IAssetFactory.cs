namespace Shared.Interfaces.Model;

/// <summary>
/// Instantiates classes for registered <see cref="Type"/>s at run-time.
/// </summary>
/// <remarks>
/// <see cref="IAssetFactory"/> is intended for use in the Data Access Layer.
/// </remarks>
public interface IAssetFactory
{
    /// <summary>
    /// Builds instances of <see cref="Type"/>s registered in a <see cref="Shared.Services.Registry.ITypeRegister{T}"/>, from data provided by a <see cref="IDataProvider"/>.
    /// </summary>
    /// <param name="typeName">The name of the registered <see cref="Type"/>.</param>
    /// <returns>An instance or collection of instances of the <see cref="Type"/> registered under <paramref name="typeName"/>,<br/>
    /// as determined by its registered <see cref="Services.Registry.RegistryRelation.ConvertedDataType"/>.</returns>
    public object? GetAsset(string typeName);
}