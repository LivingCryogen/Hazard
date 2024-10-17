using Share.Interfaces.ViewModel;

namespace Share.Interfaces;
/// <summary>
/// Defines public data for ViewModel structs representing <see cref="Model.ITroopCard"/>s.
/// </summary>
public interface ITroopCardInfo : ICardInfo
{
    /// <summary>
    /// Gets or inits the name to display in the UI for the corresponding <see cref="Model.ITroopCard"/>.
    /// </summary>
    /// <value>
    /// A <see cref="string"/>.
    /// </value>
    string DisplayName { get; init; }
    /// <summary>
    /// Gets or sets the <see cref="string">name</see> of the corresponding <see cref="Model.ITroopCard.Insigne"/>.
    /// </summary>
    /// <value>
    /// A <see cref="string"/>.
    /// </value>
    string InsigniaName { get; set; }
    /// <summary>
    /// Gets or sets the integer value of the corresponding <see cref="Model.ITroopCard.Insigne"/>.
    /// </summary>
    /// <value>
    /// An <see cref="int"/>.
    /// </value>
    int InsigniaValue { get; set; }
}
