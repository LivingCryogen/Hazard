using Shared.Services.Registry;

namespace Model.Tests.Registry.Stubs;

public class RegisterInitializerStub : IRegistryInitializer
{
    public void PopulateRegistry(ITypeRegister<ITypeRelations> typeRegister)
    {
    }
}