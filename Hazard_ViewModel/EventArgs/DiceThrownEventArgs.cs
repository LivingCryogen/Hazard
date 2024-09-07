using Hazard_Share.Interfaces.ViewModel;

namespace Hazard_ViewModel.EventArgs;

public class DiceThrownEventArgs(int[] attackResults, int[] defenseResults) : System.EventArgs, IDiceThrownEventArgs
{
    public List<int> AttackResults { get; init; } = [.. attackResults];
    public List<int> DefenseResults { get; init; } = [.. defenseResults];
}
