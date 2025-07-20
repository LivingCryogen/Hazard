using Shared.Geography.Enums;
using Shared.Interfaces.Model;

namespace Model.Entities
{
    public interface IDeck
    {
        List<ICard<TerrID>> DiscardPile { get; set; }
        List<ICard<TerrID>> Library { get; set; }

        void Discard(ICard<TerrID> card);
        void Discard(ICard<TerrID>[] cards);
        ICard<TerrID> DrawCard();
        void Shuffle();
    }
}