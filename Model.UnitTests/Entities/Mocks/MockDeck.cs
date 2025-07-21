using Model.Entities;
using Model.Tests.Fixtures.Mocks;
using Shared.Interfaces.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Tests.Entities.Mocks;

public class MockDeck : IDeck<MockTerrID>
{
    public List<ICard<MockTerrID>> DiscardPile { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public List<ICard<MockTerrID>> Library { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public void Discard(ICard<MockTerrID> card)
    {
        throw new NotImplementedException();
    }

    public void Discard(ICard<MockTerrID>[] cards)
    {
        throw new NotImplementedException();
    }

    public ICard<MockTerrID> DrawCard()
    {
        throw new NotImplementedException();
    }

    public void Shuffle()
    {
        throw new NotImplementedException();
    }
}
