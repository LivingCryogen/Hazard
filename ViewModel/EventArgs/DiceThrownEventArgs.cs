using Shared.Interfaces.ViewModel;

namespace ViewModel.EventArgs;

public class DiceThrownEventArgs(int[] attackResults, int[] defenseResults) : System.EventArgs, IDiceThrownEventArgs
{
    public int[] AttackResults { get; init; } = [.. attackResults];
    public int[] DefenseResults { get; init; } = [.. defenseResults];
}
