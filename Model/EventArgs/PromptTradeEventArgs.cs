using Shared.Interfaces.Model;

namespace Model.EventArgs;

class PromptTradeEventArgs(int player, bool forced) : System.EventArgs, IPromptTradeEventArgs
{
    /// <inheritdoc cref="IPromptTradeEventArgs.Player"/>
    public int Player { get; } = player;
    /// <inheritdoc cref="IPromptTradeEventArgs.Force"/>
    public bool Force { get; } = forced;
}
