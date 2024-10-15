using Hazard_Model.Core;
using Hazard_Model.DataAccess;
using Hazard_Share.Interfaces.Model;
using Hazard_Share.Interfaces.ViewModel;
using Hazard_Share.Services.Registry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hazard_ViewModel.Services;

public class GameService(
    ILoggerFactory loggerFactory,
    IAssetFetcher assetFetcher,
    IAssetFactory assetFactory,
    ITypeRegister<ITypeRelations> registry,
    IConfiguration config)
    : IGameService
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly IAssetFetcher _assetFetcher = assetFetcher;
    private readonly IAssetFactory _assetFactory = assetFactory;
    private readonly ITypeRegister<ITypeRelations> _registry = registry;
    private readonly IConfiguration _config = config;

    public (IGame Game, Regulator Regulator) CreateGameWithRegulator(int numPlayers)
    {
        var game = new Game(numPlayers, _loggerFactory, _assetFetcher, _registry, _config);
        Regulator regulator = new Regulator(_loggerFactory.CreateLogger<Regulator>(), game);
        regulator.Initialize();
        return (game, regulator);
    }
}
