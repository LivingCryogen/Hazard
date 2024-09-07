using Hazard_Share.Interfaces.Model;

namespace Hazard_Model.EventArgs;
/// <inheritdoc cref="IPlayerChangedEventArgs"/>
public class PlayerChangedEventArgs : System.EventArgs, IPlayerChangedEventArgs
{
    /// <summary>
    /// Creates an event arg for when an <see cref="IPlayer"/> property changes, but no stored values need to be passed. (Ie, the alert and a property name is sufficient)
    /// </summary>
    /// <param name="propName">The name of the property which changed.</param>
    public PlayerChangedEventArgs(string propName)
    {
        PropertyName = propName;
    }
    /// <summary>
    /// Creates an event arg for when an <see cref="IPlayer"/> property changes *other than* <see cref="IPlayer.Hand"/>.
    /// </summary>
    /// <param name="propName">The name of the property which changed.</param>
    /// <param name="oldValue">The old value, from before the change.</param>
    /// <param name="newValue">The new value, from after the change.</param>
    public PlayerChangedEventArgs(string propName, object? oldValue, object? newValue)
    {
        PropertyName = propName;
        OldValue = oldValue;
        NewValue = newValue;
    }
    /// <summary>
    /// Creates an event arg for when <see cref="IPlayer.Hand"/> changes.
    /// </summary>
    /// <param name="propName">The name of the property which changed.</param>
    /// <param name="oldValue">The old value, from before the change.</param>
    /// <param name="newValue">The new value, from after the change.</param>
    /// <param name="handIndex">The index at which <see cref="IPlayer.Hand"/> changed.</param>
    public PlayerChangedEventArgs(string propName, object? oldValue, object? newValue, int handIndex)
    {
        PropertyName = propName;
        OldValue = oldValue;
        NewValue = newValue;
        HandIndex = handIndex;
    }
    /// <inheritdoc cref="IPlayerChangedEventArgs.PropertyName"/>
    public string PropertyName { get; init; }
    /// <inheritdoc cref="IPlayerChangedEventArgs.OldValue"/>
    public object? OldValue { get; init; }
    /// <inheritdoc cref="IPlayerChangedEventArgs.NewValue"/>
    public object? NewValue { get; init; }
    /// <inheritdoc cref="IPlayerChangedEventArgs.HandIndex"/>
    public int? HandIndex { get; init; } = null;
}
