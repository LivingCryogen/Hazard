namespace Share.Interfaces.Model;
/// <summary>
/// An <see cref="EventArgs"/> for <see cref="IRegulator.PromptTradeIn"/>.
/// </summary>
/// <remarks>
/// Fires when a player must be prompted with the option to trade in cards.
/// </remarks>
public interface IPromptTradeEventArgs
{
    /// <summary>
    /// Gets the number of the player prompted.
    /// </summary>
    /// <value>A <see cref="int">number</see> matching the <see cref="IPlayer.Number"/> of the player to prompt.</value>
    int Player { get; }
    /// <summary>
    /// Gets a flag indicating whether the trade is forced.
    /// </summary>
    /// <value><see langword="true"/> if the player must make the trade before proceeding; otherwises, <see langword="false"/>.</value>
    bool Force { get; }
}
