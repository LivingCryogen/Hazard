using Model.Entities;
using Model.Tests.Fixtures.Mocks;
using Shared.Interfaces.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Tests.Entities.Mocks;

public class MockDeck : IDeck
{
    public List<ICard> DiscardPile { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public List<ICard> Library { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public void Discard(ICard card)
    {
        throw new NotImplementedException();
    }

    public void Discard(ICard[] cards)
    {
        throw new NotImplementedException();
    }

    public ICard DrawCard()
    {
        throw new NotImplementedException();
    }

    public void Shuffle()
    {
        throw new NotImplementedException();
    }
}
