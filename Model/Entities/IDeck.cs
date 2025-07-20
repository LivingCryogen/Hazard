namespace Shared.Interfaces.Model;

public interface IDeck<T> where T: struct, Enum
{
    List<ICard<T>> DiscardPile { get; set; }
    List<ICard<T>> Library { get; set; }

    void Discard(ICard<T> card);
    void Discard(ICard<T>[] cards);
    ICard<T> DrawCard();
    void Shuffle();
}