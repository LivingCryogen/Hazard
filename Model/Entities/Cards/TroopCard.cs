using Microsoft.Extensions.Logging;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;

namespace Model.Entities.Cards;

/// <summary>
/// Default card type implementation for the base game.
/// </summary>
public class TroopCard : ITroopCard
{
    /// <summary>
    /// Constructs a TroopCard with a logger provided by a logger Factory.
    /// </summary>
    /// <param name="loggerFactory">A logger factory which instantiates loggers for debug information and errors.</param>
    public TroopCard(ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger<TroopCard>();
    }
    /// <summary>
    /// Constructs a TroopCard with its logger.
    /// </summary>
    /// <param name="logger">A logger for debug information and errors.</param>
    public TroopCard(ILogger<TroopCard> logger)
    {
        Logger = logger;
    }
    /// <summary>
    /// Constructs a TroopCard as a member of the <paramref name="cardSet"/> collection.
    /// </summary>
    /// <param name="cardSet">The <see cref="ICardSet"/> to which this <see cref="TroopCard"/> belongs.</param>
    /// <param name="logger">A logger for debug information and errors.</param>
    public TroopCard(ICardSet cardSet, ILogger<TroopCard> logger)
    {
        CardSet = cardSet;
        ParentTypeName = cardSet.GetType().Name;
        Logger = logger;
    }
    /// <inheritdoc cref="ICard.PropertySerializableTypeMap"/>
    public Dictionary<string, Type> PropertySerializableTypeMap { get; } = new()
    {
        { nameof(Target), typeof(TerrID) },
        { nameof(Insigne), typeof(int) },
        { nameof(ParentTypeName), typeof(string) },
        { nameof(IsTradeable), typeof(bool) }
    };
    /// <inheritdoc cref="ICard.Logger"/>
    public ILogger Logger { get; set; }
    /// <inheritdoc cref="ICard.TypeName"/>
    public string TypeName { get; set; } = nameof(TroopCard);
    /// <summary>
    /// Gets or sets the name of this card's 'parent': the <see cref="ICardSet"/> that contains it.
    /// </summary>
    /// <remarks>
    /// <see cref="ICardSet.MemberTypeName"/> of the parent should be equal to "TroopCard".
    /// </remarks>
    public string ParentTypeName { get; private set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parent collection containing this <see cref="TroopCard"/> in its <see cref="ICardSet.Cards"/> list.
    /// </summary>
    /// <value>An <see cref="ICardSet"/> instance, if this and it have been initialized and mapped. Otherwise, <see langword="null"/>.</value>
    public ICardSet? CardSet { get; set; } = null;
    /// <inheritdoc cref="ICard.IsTradeable"/>
    public bool IsTradeable { get; set; } = true;
    /// <inheritdoc cref="ICard.Target"/>
    public TerrID[] Target { get; set; } = [];
    /// <inheritdoc cref="ITroopCard.Insigne"/>
    public TroopInsignia Insigne { get; set; }
    Enum ITroopCard.Insigne { get => Insigne; set { Insigne = (TroopInsignia)value; } }
}
