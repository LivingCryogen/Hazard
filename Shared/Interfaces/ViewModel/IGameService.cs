using Shared.Interfaces.Model;

namespace Shared.Interfaces.ViewModel;
/// <summary>
/// A service for injecting necessary Model objects into the ViewModel.
/// </summary>
public interface IGameService
{
    /// <summary>
    /// Initializes a Game with its Regulator.
    /// </summary>
    /// <param name="numPlayers">The number of players in the Game.</param>
    /// <returns>An initialized Game paired with its initialized Regulator.</returns>
    (IGame Game, IRegulator Regulator) CreateGameWithRegulator(int numPlayers);
}
