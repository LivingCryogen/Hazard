using Shared.Geography;
using Shared.Interfaces.Model;

namespace Model.Tests.DataAccess.Stubs;

public class AssetFetcherStub : IAssetFetcher
{
    List<ICardSet> IAssetFetcher.FetchCardSets()
    {
        throw new NotImplementedException();
    }

    GeographyInitializer IAssetFetcher.FetchGeography()
    {
        throw new NotImplementedException();
    }
}
