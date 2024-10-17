using Model.Tests.Fixtures.Mocks;
using Share.Services.Registry;

namespace Model.Tests.Fixtures;

public static class SharedRegister
{
    public static readonly MockRegistryInitializer Initializer = new();
    public static readonly TypeRegister Registry = new(Initializer);
}
