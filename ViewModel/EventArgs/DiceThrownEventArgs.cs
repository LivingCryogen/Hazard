using Shared.Interfaces.ViewModel;

namespace ViewModel.EventArgs;

public class DiceThrownEventArgs(int[] attackResults, int[] defenseResults) : System.EventArgs, IDiceThrownEventArgs
{
    public List<int> AttackResults { get; init; } = [.. attackResults];
    public List<int> DefenseResults { get; init; } = [.. defenseResults];
}
