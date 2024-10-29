using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Model.Core;
using Shared.Interfaces.Model;
using Shared.Interfaces.ViewModel;
using Shared.Services.Registry;

namespace ViewModel.Services;

public class GameService(ILoggerFactory loggerFactory, IAssetFetcher assetFetcher,
    ITypeRegister<ITypeRelations> registry, IConfiguration config)
    : IGameService
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly IAssetFetcher _assetFetcher = assetFetcher;
    private readonly ITypeRegister<ITypeRelations> _registry = registry;
    private readonly IConfiguration _config = config;

    public (IGame Game, IRegulator Regulator) CreateGameWithRegulator(int numPlayers)
    {
        var game = new Game(numPlayers, _loggerFactory, _assetFetcher, _registry, _config);
        Regulator regulator = new(_loggerFactory.CreateLogger<Regulator>(), game);
        regulator.Initialize();
        return (game, regulator);
    }
}
