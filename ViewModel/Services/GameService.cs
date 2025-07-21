using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Model.Core;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Interfaces.ViewModel;
using Shared.Services.Registry;

namespace ViewModel.Services;

public class GameService (ILoggerFactory loggerFactory,
    IAssetFetcher<TerrID> assetFetcher,
    IStatTracker<TerrID, ContID> statTracker,
    ITypeRegister<ITypeRelations> registry,
    IConfiguration config)
    : IGameService<TerrID, ContID>
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly IAssetFetcher<TerrID> _assetFetcher = assetFetcher;
    private readonly IStatTracker<TerrID, ContID> _statTracker = statTracker;
    private readonly ITypeRegister<ITypeRelations> _registry = registry;
    private readonly IConfiguration _config = config;

    public (IGame<TerrID, ContID> Game, IRegulator<TerrID, ContID> Regulator) CreateGameWithRegulator(int numPlayers)
    {
        var game = new Game(numPlayers, _loggerFactory, _assetFetcher, _statTracker, _registry, _config);
        Regulator regulator = new(_loggerFactory.CreateLogger<Regulator>(), game);
        regulator.Initialize();
        return (game, regulator);
    }
}
