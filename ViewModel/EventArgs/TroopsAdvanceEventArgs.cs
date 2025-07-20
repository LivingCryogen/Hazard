using Shared.Geography.Enums;
using Shared.Interfaces.ViewModel;

namespace ViewModel.EventArgs;

public class TroopsAdvanceEventArgs(TerrID source, TerrID target, int min, int max, bool conquered) : System.EventArgs, ITroopsAdvanceEventArgs<TerrID>
{
    public TerrID Source { get; init; } = source;
    public TerrID Target { get; init; } = target;
    public int Max { get; init; } = max;
    public int Min { get; init; } = min;
    public bool Conquered { get; init; } = conquered;
}
