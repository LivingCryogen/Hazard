using Share.Interfaces.Model;
using Share.Services.Registry;

namespace Model.Entities.Cards;

public class CardFactory(ITypeRegister<ITypeRelations> typeRegister)
{
    private readonly ITypeRegister<ITypeRelations> _registry = typeRegister;
    public ICard BuildCard(string typeName)
    {
        if (_registry[typeName] is not Type registeredType)
            throw new ArgumentException($"The provided name {typeName} was not registered in {_registry}.", nameof(typeName));
        if (Activator.CreateInstance(registeredType) is not ICard activatedCard)
            throw new InvalidOperationException($"ICard construction of type {registeredType} failed.");
        if (string.IsNullOrEmpty(activatedCard.TypeName))
            activatedCard.TypeName = registeredType.Name;
        return activatedCard;
    }
}
