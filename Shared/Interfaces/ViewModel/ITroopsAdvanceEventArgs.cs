namespace Shared.Interfaces.ViewModel;

/// <summary>
/// Contract for EventArgs used by <see cref="IMainVM.AdvanceRequest"/>.
/// </summary>
public interface ITroopsAdvanceEventArgs<T> where T : struct, Enum
{
    /// <summary>
    /// Get or inits a flag indicating if the target of the advance was conquered in attack.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the advance is ocurring after a successful attack; <see langword="false"/> if the advance is a move during <see cref="Shared.Enums.GamePhase.Move"/>.
    /// </value>
    bool Conquered { get; init; }
    /// <summary>
    /// Gets or inits the maximum number of armies that may be advanced.
    /// </summary>
    /// <value>
    /// A number >= the number of dice used by the attacker.
    /// </value>
    int Max { get; init; }
    /// <summary>
    /// Gets or inits the minimum number of armies that may be advanced.
    /// </summary>
    /// <value>
    /// A number >= the number of dice used by the attacker.
    /// </value>
    int Min { get; init; }
    /// <summary>
    /// Gets or inits the source territory's Enum ID.
    /// </summary>
    T Source { get; init; }
    /// <summary>
    /// Gets or inits the target territory's Enum ID.
    /// </summary>
    T Target { get; init; }
}