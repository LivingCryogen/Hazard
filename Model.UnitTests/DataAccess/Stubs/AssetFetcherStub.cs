using Shared.Geography;
using Shared.Interfaces.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    IRuleValues IAssetFetcher.FetchRuleValues()
    {
        throw new NotImplementedException();
    }
}
