﻿using Hazard_Share.Interfaces.Model;
using Hazard_Share.Services.Registry;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hazard_Model.Entities.Cards;

public class CardFactory(ITypeRegister<ITypeRelations> typeRegister)
{
    private readonly ITypeRegister<ITypeRelations> _registry = typeRegister;
    public ICard BuildCard(string typeName)
    {
        if (_registry[typeName] is not Type registeredType)
            throw new ArgumentException($"The provided name {typeName} was not registered in {_registry}.", nameof(typeName));
        if (Activator.CreateInstance(registeredType) is not ICard activatedCard)
            throw new InvalidOperationException($"ICard construction of type {registeredType} failed.");
        return activatedCard;
    }
}