using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model.Core;
using Shared.Interfaces.Model;
using Shared.Interfaces.ViewModel;
using Shared.Services.Configuration;
using Shared.Services.Registry;

namespace ViewModel.Services;

public class GameService(ILoggerFactory loggerFactory,
    IAssetFetcher assetFetcher,
    ITypeRegister<ITypeRelations> registry,
    IOptions<AppConfig> options,
    IRuleValues ruleValues)

    : IGameService
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly IAssetFetcher _assetFetcher = assetFetcher;
    private readonly ITypeRegister<ITypeRelations> _registry = registry;
    private readonly IOptions<AppConfig> _options = options;

    public (IGame Game, IRegulator Regulator) CreateGameWithRegulator(int numPlayers)
    {
        var game = new Game(numPlayers, _loggerFactory, _assetFetcher, _registry, _options, ruleValues);
        Regulator regulator = new(_loggerFactory.CreateLogger<Regulator>(), game);
        regulator.Initialize();
        return (game, regulator);
    }
}
