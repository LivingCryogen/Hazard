using Model.Tests.Fixtures.Mocks;
using Shared.Geography;
using Shared.Interfaces.Model;

namespace Model.Tests.DataAccess.Stubs;

public class AssetFetcherStub : IAssetFetcher<MockTerrID>
{
    List<ICardSet<MockTerrID>> IAssetFetcher<MockTerrID>.FetchCardSets()
    {
        throw new NotImplementedException();
    }

    GeographyInitializer IAssetFetcher<MockTerrID>.FetchGeography()
    {
        throw new NotImplementedException();
    }
}
