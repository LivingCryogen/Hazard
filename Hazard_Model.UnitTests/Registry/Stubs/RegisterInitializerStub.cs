using Hazard_Share.Services.Registry;

namespace Hazard_Model.Tests.Registry.Stubs;

public class RegisterInitializerStub : IRegistryInitializer
{
    public void PopulateRegistry(ITypeRegister<ITypeRelations> typeRegister)
    { }
}