namespace Share.Interfaces.ViewModel;

/// <summary>
/// Encapsulates data for <see cref="System.EventArgs"/> used by <see cref="IMainVM.AdvanceRequest"/>.
/// </summary>
public interface ITroopsAdvanceEventArgs
{
    /// <summary>
    /// Get or inits a flag indicating if the target of the advance was conquered in attack.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the advance is ocurring after a successful attack; <see langword="false"/> if the advance is a move during <see cref="Share.Enums.GamePhase.Move"/>.
    /// </value>
    bool Conquered { get; init; }
    /// <summary>
    /// Gets or inits the maximum number of armies that may be advanced.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> at >= the number of dice used by the attacker.
    /// </value>
    int Max { get; init; }
    /// <summary>
    /// Gets or inits the minimum number of armies that may be advanced.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> at >= the number of dice used by the attacker.
    /// </value>
    int Min { get; init; }
    /// <summary>
    /// Gets or inits the <see cref="int"/> value of the source territory's <see cref="Share.Enums.TerrID"/>.
    /// </summary>
    /// <value>
    /// An <see cref="int"/>.
    /// </value>
    int Source { get; init; }
    /// <summary>
    /// Gets or inits the <see cref="int"/> value of the target territory's <see cref="Share.Enums.TerrID"/>.
    /// </summary>
    /// <value>
    /// An <see cref="int"/>.
    /// </value>
    int Target { get; init; }
}