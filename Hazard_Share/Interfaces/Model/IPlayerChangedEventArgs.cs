namespace Hazard_Share.Interfaces.Model;
/// <summary>
/// An <see cref="System.EventArgs"/> for when an <see cref="IPlayer"/> changes.
/// </summary>
/// <remarks>
/// To be used primarily by <see cref="ViewModel.IPlayerData"/>.
/// </remarks>
public interface IPlayerChangedEventArgs
{
    /// <summary>
    /// Gets or inits the new value of a change.
    /// </summary>
    /// <value>An <see cref="object"/> containing the value, if any; otherwise, <see langword="null"/>.</value>
    object? NewValue { get; init; }
    /// <summary>
    /// Gets or inits the old value of a change.
    /// </summary>
    /// <value>An <see cref="object"/> containing the value, if any; otherwise, <see langword="null"/>.</value>
    object? OldValue { get; init; }
    /// <summary>
    /// Gets or inits the index of the hand collection which changed.
    /// </summary>
    /// <value>An <see cref="int"/> index, if the <see cref="IPlayer.Hand"/> changed; otherwise, <see langword="null"/>.</value>
    public int? HandIndex { get; init; }
    /// <summary>
    /// Gets or inits the name of the property which changed.
    /// </summary>
    string PropertyName { get; init; }
}