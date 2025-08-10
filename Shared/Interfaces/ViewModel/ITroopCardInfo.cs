using Shared.Interfaces.ViewModel;

namespace Shared.Interfaces;
/// <summary>
/// Defines public data for ViewModel structs representing <see cref="Model.ITroopCard"/>s.
/// </summary>
public interface ITroopCardInfo<T, U> : ICardInfo where T : struct, Enum where U : struct, Enum
{
    /// <summary>
    /// Gets or inits the name to display in the UI for the corresponding <see cref="Model.ITroopCard"/>.
    /// </summary>
    string DisplayName { get; init; }
    /// <summary>
    /// Gets or sets the name of the corresponding <see cref="Model.ITroopCard.Insigne"/>.
    /// </summary>
    string InsigniaName { get; set; }
    /// <summary>
    /// Gets or sets the integer value of the corresponding <see cref="Model.ITroopCard.Insigne"/>.
    /// </summary>
    int InsigniaValue { get; set; }
}
