
namespace Shared.Interfaces.ViewModel;
/// <summary>
/// Contract for EventArgs used by <see cref="IMainVM.DiceThrown"/>.
/// </summary>
public interface IDiceThrownEventArgs
{
    /// <summary>
    /// Gets or inits the numerical results of the attacker's "dice rolls."
    /// </summary>
    List<int> AttackResults { get; init; }
    /// <summary>
    /// Gets or inits the numerical results of the defender's "dice rolls."
    /// </summary>
    List<int> DefenseResults { get; init; }
}