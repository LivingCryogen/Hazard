using Hazard_Model.Entities;
using Microsoft.Extensions.Logging;

namespace Hazard_Model.Tests.Entities.Mocks;

public class MockCardBase(ILogger _logger) : CardBase(_logger)
{
    public void Reset()
    {
        Sets = [];
        GameDeck = new();
    }
}
