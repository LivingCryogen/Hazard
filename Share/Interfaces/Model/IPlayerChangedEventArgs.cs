namespace Share.Interfaces.Model;
/// <summary>
/// A recipe for <see cref="EventArgs"/> used by <see cref="IPlayer.PlayerChanged"/>.
/// </summary>
/// <remarks>
/// To be used primarily by <see cref="ViewModel.IPlayerData"/>.
/// </remarks>
public interface IPlayerChangedEventArgs
{
    /// <summary>
    /// Gets or inits the new value after a change to a property on <see cref="IPlayer"/>.
    /// </summary>
    /// <value>The new value of the property, if any; otherwise, <see langword="null"/>.</value>
    object? NewValue { get; init; }
    /// <summary>
    /// Gets or inits the old value after a change to a property on <see cref="IPlayer"/>.
    /// </summary>
    /// <value>The old value of the property, if any; otherwise, <see langword="null"/>.</value>
    object? OldValue { get; init; }
    /// <summary>
    /// Gets or inits the index of the hand collection which changed.
    /// </summary>
    /// <value>An <see cref="int">index</see>, if the <see cref="IPlayer.Hand"/> changed; otherwise, <see langword="null"/>.</value>
    public int? HandIndex { get; init; }
    /// <summary>
    /// Gets or inits the name of the property which changed.
    /// </summary>
    string PropertyName { get; init; }
}