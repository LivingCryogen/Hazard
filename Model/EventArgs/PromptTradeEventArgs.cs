using Share.Interfaces.Model;

namespace Model.EventArgs;

class PromptTradeEventArgs(int player, bool forced) : System.EventArgs, IPromptTradeEventArgs
{
    public int Player { get; } = player;
    public bool Force { get; } = forced;
}
