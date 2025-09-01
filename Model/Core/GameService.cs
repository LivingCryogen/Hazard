using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model.Stats;
using Shared.Interfaces.Model;
using Shared.Services.Configuration;
using Shared.Services.Registry;

namespace Model.Core;

public class GameService(ILoggerFactory loggerFactory,
    IAssetFetcher assetFetcher,
    ITypeRegister<ITypeRelations> registry,
    IOptions<AppConfig> options,
    IRuleValues ruleValues,
    IStatRepo statRepo
    )
    : IGameService
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly IAssetFetcher _assetFetcher = assetFetcher;
    private readonly ITypeRegister<ITypeRelations> _registry = registry;
    private readonly IOptions<AppConfig> _options = options;
    private readonly IStatRepo _statRepo = statRepo;

    public (IGame Game, IRegulator Regulator) CreateGameWithRegulator(int numPlayers)
    {
        var game = new Game(numPlayers, _loggerFactory, _assetFetcher, _registry, _options, ruleValues);
        Regulator regulator = new(_loggerFactory.CreateLogger<Regulator>(), game);
        regulator.Initialize();
        _statRepo.CurrentTracker = game.StatTracker;
        return (game, regulator);
    }

    public IGame CreateGame(int numPlayers)
    {
        return new Game(numPlayers, _loggerFactory, _assetFetcher, _registry, _options, ruleValues);
    }
}
