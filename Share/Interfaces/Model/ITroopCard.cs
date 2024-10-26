namespace Share.Interfaces.Model;
/// <summary><see cref="ICard"/> extension for the default card type included in the base game: <see cref="ITroopCard"/>.</summary>
/// <inheritdoc cref="ICard"/>
public interface ITroopCard : ICard
{
    /// <summary>
    /// Gets or sets an insignia value for the card. 
    /// </summary>
    Enum Insigne { get; set; }
}
