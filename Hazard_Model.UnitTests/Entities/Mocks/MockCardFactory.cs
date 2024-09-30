using Hazard_Share.Interfaces.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hazard_Model.Tests.Entities.Mocks;

public class MockCardFactory
{
    public ICard BuildCard(string typeName)
    {
        return new MockCard();
    }
}
