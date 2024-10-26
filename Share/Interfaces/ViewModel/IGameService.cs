using Share.Interfaces.Model;

namespace Share.Interfaces.ViewModel;
/// <summary>
/// A service for injecting necessary Model objects into the ViewModel.
/// </summary>
public interface IGameService
{
    /// <summary>
    /// Initializes a Game with its Regulator.
    /// </summary>
    /// <param name="numPlayers">The <see cref="int">number</see> of players in the Game.</param>
    /// <returns>A <see cref="Tuple{T1, T2}"/> containing the initialized Game and the initialized Regulator, respectively.</returns>
    (IGame Game, IRegulator Regulator) CreateGameWithRegulator(int numPlayers);
}
