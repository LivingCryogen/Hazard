using Share.Interfaces.ViewModel;

namespace ViewModel.EventArgs;

public class TroopsAdvanceEventArgs(int source, int target, int min, int max, bool conquered) : System.EventArgs, ITroopsAdvanceEventArgs
{
    public int Source { get; init; } = source;
    public int Target { get; init; } = target;
    public int Max { get; init; } = max;
    public int Min { get; init; } = min;
    public bool Conquered { get; init; } = conquered;
}
