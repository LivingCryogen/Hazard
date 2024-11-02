namespace Shared.Services.Registry;
/// <summary>
/// Provides default values to <see cref="ITypeRegister{T}"/> instances.
/// </summary>
public interface IRegistryInitializer
{
    /// <summary>
    /// Populate a <see cref="ITypeRegister{T}"/> using custom logic.
    /// </summary>
    /// <param name="registry"></param>
    public void PopulateRegistry(ITypeRegister<ITypeRelations> registry);
}