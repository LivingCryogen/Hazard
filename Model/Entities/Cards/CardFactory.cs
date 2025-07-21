using Microsoft.Extensions.Logging;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Registry;

namespace Model.Entities.Cards;
/// <summary>
/// Activates <see cref="ICard">card</see> instances based on registry data.
/// </summary>
/// <remarks>
/// Useful for objects who need to create them from save file data.
/// </remarks>
/// <param name="typeRegister">The application's type registry, which must have valid entries for the factory to function.</param>
/// <param name="loggerFactory">The factory for providing loggers to the <see cref="ICard">card</see> instances.</param>
public class CardFactory(ITypeRegister<ITypeRelations> typeRegister, ILoggerFactory loggerFactory) : ICardFactory<TerrID>
{
    private readonly ITypeRegister<ITypeRelations> _registry = typeRegister;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    /// <summary>
    /// Activates a <see cref="ICard">card</see>.
    /// </summary>
    /// <param name="typeName">The name of the card's type as registered in <see cref="ITypeRegister{T}"/>.</param>
    /// <returns>The activated <see cref="ICard">card</see>.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="typeName"/> is not registered.</exception>
    /// <exception cref="InvalidOperationException">Thrown if activation of the <see cref="Type"/> provided by the registry fails.</exception>
    public ICard<TerrID> BuildCard(string typeName)
    {
        if (_registry[typeName] is not Type registeredType)
            throw new ArgumentException($"The provided name {typeName} was not registered in {_registry}.", nameof(typeName));
        if (Activator.CreateInstance(registeredType, [_loggerFactory]) is not ICard<TerrID> activatedCard)
            throw new InvalidOperationException($"ICard construction of type {registeredType} failed.");
        if (string.IsNullOrEmpty(activatedCard.TypeName))
            activatedCard.TypeName = registeredType.Name;
        return activatedCard;
    }
}
