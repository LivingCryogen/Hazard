using Shared.Geography.Enums;
using Shared.Interfaces.Model;

namespace Model.Entities;
/// <inheritdoc cref="IDeck{T}"/>
public class Deck : IDeck<TerrID>
{
    /// <summary>
    /// Constructs an empty deck.
    /// </summary>
    public Deck()
    {
        Library = [];
        DiscardPile = [];
    }
    /// <summary>
    /// Constructs a deck from a group of cards, placing them all in the <see cref="Library"/>.
    /// </summary>
    /// <param name="cards">The cards that will constitute the deck.</param>
    public Deck(ICard<TerrID>[] cards)
    {
        Library = [.. cards];
        DiscardPile = [];
    }
    /// <inheritdoc />
    /// <remarks>
    /// This constructor is useful when a <see cref="Deck"/> is to be built from multiple <see cref="ICardSet.Cards"/> values.
    /// </remarks>
    public Deck(ICard<TerrID>[][] cards)
    {
        Library = [.. cards.SelectMany(set => set.Select(card => card))];
        DiscardPile = [];
    }
    /// <param name="cardSet">A card set, whose property <see cref="ICardSet{T}.Cards"/> contains the <see cref="Card{T}"/>s that will constitute the deck.</param>
    /// <inheritdoc cref="Deck(ICard{TerrID}[])"/>
    public Deck(ICardSet<TerrID> cardSet)
    {
        Library = [.. cardSet?.Cards ?? Enumerable.Empty<ICard<TerrID>>()];
        DiscardPile = [];
    }
    /// <param name="cardSets">An array of card sets, each of whose property <see cref="ICardSet{T}.Cards"/> contains <see cref="ICard{T}"/>s that will constitute the deck.</param>
    /// <inheritdoc cref="Deck(ICard{TerrID}[])"/>
    public Deck(ICardSet<TerrID>[] cardSets)
    {
        Library =
            [..
                cardSets?.SelectMany(item =>
                    (item?.Cards ?? Enumerable.Empty<ICard<TerrID>>()))
                ??
                []
            ];
        DiscardPile = [];
    }
    /// <inheritdoc cref="IDeck{T}.Library"/>/>
    public List<ICard<TerrID>> Library { get; set; }
    /// <inheritdoc cref="IDeck{T}.DiscardPile"/>
    public List<ICard<TerrID>> DiscardPile { get; set; }
    /// <inheritdoc cref="IDeck{T}.DrawCard"/>
    public ICard<TerrID> DrawCard()
    {
        if (Library.Count + DiscardPile.Count <= 0)
            throw new InvalidOperationException("An attempt was made to draw a card from an empty deck.");
        if (Library.Count <= 0)
            Shuffle();

        ICard<TerrID> drawn = Library[^1];
        Library.Remove(Library[^1]);
        return drawn;
    }
    /// <inheritdoc cref="IDeck{T}.Discard(ICard{T})"/>
    public void Discard(ICard<TerrID> card)
    {
        DiscardPile.Add(card);
    }
    /// <inheritdoc cref="IDeck{T}.Discard(ICard{T}[])"/>/>
    public void Discard(ICard<TerrID>[] cards)
    {
        DiscardPile.AddRange(cards);
    }
    /// <inheritdoc cref="IDeck{T}.Shuffle"/>
    public void Shuffle()
    {
        if (DiscardPile.Count > 0)
        {
            Library.AddRange(DiscardPile);
            DiscardPile.Clear();
        }
        if (Library.Count <= 0)
            return;

        Random randGen = new();
        for (int i = Library.Count - 1; i >= 1; i--)
        {
            int j = randGen.Next(0, i + 1);
            (Library[j], Library[i]) = (Library[i], Library[j]);
        }
    }
}
