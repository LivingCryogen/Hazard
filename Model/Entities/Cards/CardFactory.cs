using Microsoft.Extensions.Logging;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Registry;

namespace Model.Entities.Cards;
/// <inheritdoc cref="ICardFactory{T}"/>
/// /// <param name="typeRegister">The application's type registry, which must have valid entries for the factory to function.</param>
/// <param name="loggerFactory">The factory for providing loggers to the <see cref="ICard{T}">card</see> instances.</param>
public class CardFactory(ITypeRegister<ITypeRelations> typeRegister, ILoggerFactory loggerFactory) : ICardFactory<TerrID>
{
    private readonly ITypeRegister<ITypeRelations> _registry = typeRegister;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    /// <inheritdoc cref="ICardFactory{T}.BuildCard(string)"/>/>
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
