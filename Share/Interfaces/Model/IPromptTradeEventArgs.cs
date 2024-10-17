namespace Share.Interfaces.Model;
/// <summary>
/// A <see cref="System.EventArgs"/> which fires when a player must be prompted with the option to trade in cards.
/// </summary>
public interface IPromptTradeEventArgs
{
    /// <summary>
    /// Gets the number of the player prompted.
    /// </summary>
    /// <value>An <see cref="int"/> between 0 and 5.</value>
    int Player { get; }
    /// <summary>
    /// Gets a flag indicating whether the trade is forced.
    /// </summary>
    /// <value><see langword="true"/> if the player must make the trade before proceeding; otherwises, <see langword="false"/>.</value>
    bool Force { get; }
}
