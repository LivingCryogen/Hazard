namespace Hazard_Share.Enums;
/// <summary>
/// Categorizes the phases of the game state.
/// </summary>
/// Rules stipulate that, after setup, a Player's Turn consists of, in order: <see cref="GamePhase.Place"/>, <see cref="GamePhase.Attack"/>, 
/// and <see cref="GamePhase.Move"/>.
/// Note: This is a bit clunky -- especially with the new addition of "TwoPlayerSetup" -- but I'm not yet sure how to streamline things.
public enum GamePhase : int
{
    /// <summary>
    /// The game is in a null state; e.g. uninitialized
    /// </summary>  
    Null = -2,
    /// <summary>
    /// The game is in two-player setup.
    /// </summary>
    TwoPlayerSetup = -1,
    /// <summary>
    /// The game is in the default multiplayer setup.
    /// </summary>
    DefaultSetup = 0,
    /// <summary>
    /// The game is recording placement of Player armies.
    /// </summary>
    Place = 1,
    /// <summary>
    /// The game is simulating and recording Player attacks.
    /// </summary>
    Attack = 2,
    /// <summary>
    /// The game is recording Player troop movements.
    /// </summary>
    Move = 3,
    /// <summary>
    /// The game has completed.
    /// </summary>
    GameOver = 4
}
