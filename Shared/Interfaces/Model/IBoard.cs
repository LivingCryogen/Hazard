using Shared.Geography.Enums;

namespace Shared.Interfaces.Model;

/// <summary>
/// Encapsulates Data and Actions that directly affect the armies, territories, and continents on the game board.
/// </summary>
public interface IBoard<T, U> : IBinarySerializable where T: struct, Enum where U : struct, Enum
{
    /// <summary>
    /// Notifies <see cref="ViewModel.IMainVM"/> that a territory's owner or armies have changed.
    /// </summary>
    /// <remarks>
    /// Should be invoked manually when affecting changes on <see cref="Armies"/> or <see cref="TerritoryOwner"/>.
    /// </remarks>
    event EventHandler<ITerritoryChangedEventArgs<T>> TerritoryChanged;
    /// <summary>
    /// Notifies <see cref="ViewModel.IMainVM"/> that a continent's owner has changed.
    /// </summary>
    /// <remarks>
    /// Should be invoked manually when affecting changes on <see cref="ContinentOwner"/>.
    /// </remarks>
    event EventHandler<IContinentOwnerChangedEventArgs<U>>? ContinentOwnerChanged;

    /// <summary>
    /// Contains the number of armies in each territory.
    /// </summary>
    Dictionary<T, int> Armies { get; }
    /// <summary>
    /// Contains the player number of the owner of each territory.
    /// </summary>
    Dictionary<T, int> TerritoryOwner { get; }
    /// <summary>
    /// Contains the player number of the owner of each continent.
    /// </summary>
    Dictionary<U, int> ContinentOwner { get; }
    /// <summary>
    /// Gets a list of territories or of continents on the board owned by a specfied player.
    /// </summary>
    /// <param name="playerNumber">The number of the specified player.</param>
    /// <param name="enumName">The name of either <see cref="TerrID"/> or <see cref="ContID"/>, for territories owner or continents owned, respectively.</param>
    /// <returns>A list of all territories OR a list of all continents owned by player <paramref name="playerNumber"/>.</returns>
    List<object> this[int playerNumber, string enumName] { get; }
    /// <summary>
    /// Updates the board state when a player claims a territory for the first time.
    /// </summary>
    /// <remarks>
    /// This change should also fire <see cref="TerritoryChanged"/> and subsequent events.
    /// </remarks>
    /// <param name="newPlayer">The number of the player that takes the territory.<paramref name="newPlayer"/></param>
    /// <param name="territory">The ID of the territory taken control of by <paramref name="newPlayer"/>.</param>
    void Claims(int newPlayer, T territory);
    /// <inheritdoc />
    /// <param name="newPlayer"></param>
    /// <param name="territory"></param>
    /// <param name="armies">The number of armies the new owner controls in the territory.</param>
    /// <remarks>This is a variation on <see cref="Claims(int, TerrID)"/> meant to enable overriding the default one army per claim.</remarks>
    void Claims(int newPlayer, T territory, int armies);
    /// <summary>
    /// Increments the armies present within a territory.
    /// </summary>
    /// <param name="territory">The ID of the territory in <see cref="Armies"/> to increment.</param>
    void Reinforce(T territory);
    /// <summary>
    /// Increases the armies present within a territory by a specified amount.
    /// </summary>
    /// <param name="territory">The ID of the territory in <see cref="Armies"/> to increase.</param>
    /// <param name="armies">The value of the increase of armies in the <paramref name="territory"/>.</param>
    void Reinforce(T territory, int armies);
    /// <summary>
    /// Changes ownership of a territory after a successful attack.
    /// </summary>
    /// <param name="source">The territory from which the attack originated.</param>
    /// <param name="target">The territory that was attacked and is being conquered.</param>
    /// <param name="newOwner">The <see cref="IPlayer.Number"/> of the owner after the attack is completed.</param>
    /// <param name="contFlipped">If a Continent changed hands due to this conquest, its ID; otherwise, <see langword="null"/>.</param>
    void Conquer(T source, T target, int newOwner, out U? contFlipped);
    /// <summary>
    /// Determines whether a continent has changed ownership after a change in territory ownership.
    /// </summary>
    /// <remarks>The new owner is not needed here so long as <see cref="TerritoryOwner"/> is changed properly before this method is called.</remarks>
    /// <param name="changed">The territory that changed hands.</param>
    /// <param name="previousOwner">The <see cref="IPlayer.Number"/> of the territory's owner before the change.</param>
    /// <returns>The ID of the Continent that flipped, if any; otherwise, <see langword="null"/>.</s</returns>
    ContID? CheckContinentFlip(T changed, int previousOwner);
}