using Hazard_Model.Tests.Fixtures.Mocks;
using Hazard_Share.Services.Registry;

namespace Hazard_Model.Tests.Fixtures;

public static class SharedRegister
{
    public static readonly MockRegistryInitializer Initializer = new();
    public static readonly TypeRegister Registry = new(Initializer);
}
