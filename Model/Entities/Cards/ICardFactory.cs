using Shared.Interfaces.Model;
using Shared.Services.Registry;

namespace Model.Entities.Cards;
/// <summary>
/// Activates <see cref="ICard">card</see> instances based on registry data.
/// </summary>
/// <remarks>
/// Useful for objects who need to create them from save file data.
/// </remarks>
public interface ICardFactory
{
    /// <summary>
    /// Activates a <see cref="ICard">card</see>.
    /// </summary>
    /// <param name="typeName">The name of the card's type as registered in <see cref="ITypeRegister{T}"/>.</param>
    /// <returns>The activated <see cref="ICard">card</see>.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="typeName"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">Thrown if activation of the <see cref="Type"/> provided by the registry fails.</exception>
    ICard BuildCard(string typeName);
}