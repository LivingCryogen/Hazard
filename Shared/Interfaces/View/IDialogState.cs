namespace Shared.Interfaces.View;
/// <summary>
/// Lets the ViewModel know if a dialog window is open. 
/// </summary>
/// <remarks>
/// Certain commands may be enabled or disabled based on whether dialogs are open (since they await Player input).
/// </remarks>
public interface IDialogState
{
    /// <summary>
    /// Gets a flag indicating whether a dialog window is open.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if a dialog window is open at the View level; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsDialogOpen { get; }
}
