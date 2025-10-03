using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model.Stats;
using Shared.Interfaces.Model;
using Shared.Services.Configuration;
using Shared.Services.Registry;

namespace Model.Core;

/// <summary>
/// Creates game instances and wires them to their associated regulators, together with the statistics repository.
/// </summary>
/// <remarks>This service acts as a factory for creating game instances and regulators. It also integrates with the statistics repository to maintain
/// knowledge of local stat-trackers, which track game-specific statistical data.</remarks>
/// <param name="loggerFactory">The logger factory from DI.</param>
/// <param name="assetFetcher">Connection to the surface of the DAL (fetches local assets).</param>
/// <param name="registry">The application registry.</param>
/// <param name="options">Application options from DI, configuration.</param>
/// <param name="ruleValues">Post-configured rule values.</param>
/// <param name="statRepo">The local statistics repository.</param>
public class GameService(ILoggerFactory loggerFactory,
    IAssetFetcher assetFetcher,
    ITypeRegister<ITypeRelations> registry,
    IOptions<AppConfig> options,
    IRuleValues ruleValues,
    IStatRepo statRepo)
    : IGameService
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly IAssetFetcher _assetFetcher = assetFetcher;
    private readonly ITypeRegister<ITypeRelations> _registry = registry;
    private readonly IOptions<AppConfig> _options = options;
    private readonly IStatRepo _statRepo = statRepo;

    /// <summary>
    /// Creates a new game instance, with its associated regulator, and sets the stat repo to follow its stat tracker.
    /// </summary>
    /// <param name="numPlayers">The number of players in the game.</param>
    /// <returns>Initialized and wired-up game and regulator.</returns>
    public (IGame Game, IRegulator Regulator) CreateGameWithRegulator(int numPlayers)
    {
        var game = new Game(numPlayers, _loggerFactory, _assetFetcher, _registry, _options, ruleValues);
        Regulator regulator = new(_loggerFactory.CreateLogger<Regulator>(), game);
        regulator.Initialize();
        _statRepo.CurrentTracker = game.StatTracker;
        return (game, regulator);
    }
}
