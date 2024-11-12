namespace Shared.Services.Registry;
/// <summary>
/// Provides default values to <see cref="ITypeRegister{T}"/>.
/// </summary>
public interface IRegistryInitializer
{
    /// <summary>
    /// Populate a <see cref="ITypeRegister{T}"/> using custom logic.
    /// </summary>
    /// <param name="registry">The registry to initialize.</param>
    public void PopulateRegistry(ITypeRegister<ITypeRelations> registry);
}