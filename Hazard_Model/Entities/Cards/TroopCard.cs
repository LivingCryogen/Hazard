using Hazard_Share.Enums;
using Hazard_Share.Interfaces.Model;
using Microsoft.Extensions.Logging;

namespace Hazard_Model.Entities.Cards;

/// <summary>
/// Implementation of the default cards of the base game.
/// </summary>
public class TroopCard : ITroopCard
{
    /// <summary>
    /// Constructs an empty <see cref="TroopCard"/>.
    /// </summary>
    public TroopCard(ILogger<TroopCard> logger)
    { 
        Logger = logger;
    }
    /// <summary>
    /// Constructs a <see cref="TroopCard"/> as a member of the <paramref name="cardSet"/> collection.
    /// </summary>
    /// <param name="cardSet">The <see cref="ICardSet"/> to which this <see cref="TroopCard"/> belongs.</param>
    public TroopCard(ICardSet cardSet, ILogger<TroopCard> logger)
    {
        CardSet = cardSet;
        ParentTypeName = cardSet.GetType().Name;
        Logger = logger;
    }
    /// <inheritdoc cref="ICard.PropertySerializableTypeMap"/>
    public Dictionary<string, Type> PropertySerializableTypeMap { get; } = new()
    {
        { nameof(Target), typeof(int) },
        { nameof(Insigne), typeof(int) },
        { nameof(ParentTypeName), typeof(string) },
        { nameof(IsTradeable), typeof(bool) }
    };
    public ILogger Logger { get; init; }
    public string TypeName { get; set; } = nameof(TroopCard);
    /// <summary>
    /// Gets or sets the name of a <see cref="TroopCard"/>'s 'parent': the <see cref="ICardSet"/> that contains it.
    /// </summary>
    /// <remarks>
    /// <see cref="ICardSet.MemberTypeName"/> of the parent should be equal to "TroopCard".
    /// </remarks>
    /// <value>
    /// A string.
    /// </value>
    public string ParentTypeName { get; private set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parent collection containing this <see cref="TroopCard"/> in its <see cref="ICardSet.Cards"/> list.
    /// </summary>
    /// <value>An <see cref="ICardSet"/> instance, if this and it have been initialized and mapped. Otherwise, <see langword="null"/>.</value>
    public ICardSet? CardSet { get; set; } = null;
    /// <summary>
    /// Gets or sets a flag indicating that this <see cref="TroopCard"/> may be traded in. <br/>
    /// See <see cref="IRegulator.TradeInCards(int, int[])"/> and <see cref="ICardSet.IsValidTrade(ICard[])"/>.
    /// </summary>
    public bool IsTradeable { get; set; } = true;
    /// <inheritdoc cref="ICard.Target"/>
    public TerrID[] Target { get; set; } = [];
    /// <inheritdoc cref="ITroopCard.Insigne"/>
    public TroopInsignia Insigne { get; set; }
    Enum ITroopCard.Insigne { get => Insigne; set { Insigne = (TroopInsignia)value; } }
}
