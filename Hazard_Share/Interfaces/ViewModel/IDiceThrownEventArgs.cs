
namespace Hazard_Share.Interfaces.ViewModel;
/// <summary>
/// Encapsulates data for <see cref="System.EventArgs"/> used by <see cref="IMainVM.DiceThrown"/>.
/// </summary>
public interface IDiceThrownEventArgs
{
    /// <summary>
    /// Gets or inits a list of numerical results of the attacker's "dice rolls."
    /// </summary>
    /// <value>
    /// A <see cref="List{T}"/> of <see cref="int"/>.
    /// </value>
    List<int> AttackResults { get; init; }
    /// <summary>
    /// Gets or inits a list of numerical results of the defender's "dice rolls."
    /// </summary>
    /// <value>
    /// A <see cref="List{T}"/> of <see cref="int"/>.
    /// </value>
    List<int> DefenseResults { get; init; }
}