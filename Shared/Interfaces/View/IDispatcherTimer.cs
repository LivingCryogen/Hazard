namespace Shared.Interfaces.View;
/// <summary>
/// A service porting/exposing a Timer from the View/WPF level to the ViewModel.
/// </summary>
/// <remarks>
/// Currently, attacks are limited by a command disable with a WPF timer. This feels like a kludge, <br/>
/// but it may or may not be worth changing in the future.
/// </remarks>
public interface IDispatcherTimer
{
    /// <summary>
    /// Gets or sets the interval that the timer will run.
    /// </summary>
    /// <value>
    /// A <see cref="TimeSpan"/>.
    /// </value>
    TimeSpan Interval { get; set; }
    /// <summary>
    /// Gets or sets a flag indicating whether the timer is enabled.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the timer is enabled; otherwise, <see langword="false"/>.
    /// </value>
    bool IsEnabled { get; set; }
    /// <summary>
    /// Fires when the timing interval has completed.
    /// </summary>
    event EventHandler Tick;
    /// <summary>
    /// Starts the timer.
    /// </summary>
    void Start();
    /// <summary>
    /// Stops the timer.
    /// </summary>
    void Stop();
}
