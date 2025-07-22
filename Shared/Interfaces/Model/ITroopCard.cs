namespace Shared.Interfaces.Model;
/// <summary><see cref="$1ICard{T}$2"/> extension for the default card type included in the base game.</summary>
/// <inheritdoc cref="ICard"/>
public interface ITroopCard<T> : ICard<T> where T : struct, Enum
{
    /// <summary>
    /// Gets or sets an insignia value for the card. 
    /// </summary>
    Enum Insigne { get; set; }
}
