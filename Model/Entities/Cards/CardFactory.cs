using Microsoft.Extensions.Logging;
using Shared.Interfaces.Model;
using Shared.Services.Registry;

namespace Model.Entities.Cards;
/// <inheritdoc cref="ICardFactory"/>
/// /// <param name="typeRegister">The application's type registry, which must have valid entries for the factory to function.</param>
/// <param name="loggerFactory">The factory for providing loggers to the <see cref="ICard">card</see> instances.</param>
public class CardFactory(ITypeRegister<ITypeRelations> typeRegister, ILoggerFactory loggerFactory) : ICardFactory
{
    private readonly ITypeRegister<ITypeRelations> _registry = typeRegister;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    /// <inheritdoc cref="ICardFactory.BuildCard(string)"/>/>
    public ICard BuildCard(string typeName)
    {
        if (_registry[typeName] is not Type registeredType)
            throw new ArgumentException($"The provided name {typeName} was not registered in {_registry}.", nameof(typeName));
        if (Activator.CreateInstance(registeredType, [_loggerFactory]) is not ICard activatedCard)
            throw new InvalidOperationException($"ICard construction of type {registeredType} failed.");
        if (string.IsNullOrEmpty(activatedCard.TypeName))
            activatedCard.TypeName = registeredType.Name;
        return activatedCard;
    }
}
