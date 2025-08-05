using Microsoft.Extensions.Logging;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;

namespace Model.Entities.Cards;

/// <summary>
/// Default card type implementation for the base game.
/// </summary>
public class TroopCard : ITroopCard<TerrID>
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
    /// <inheritdoc cref="ICard{T}.SerializablePropertyNames"/>
    public HashSet<string> SerializablePropertyNames { get; } = [nameof(Target), nameof(Insigne), nameof(ParentTypeName), nameof(IsTradeable)];
    /// <inheritdoc cref="ICard{T}.Logger"/>
    public ILogger Logger { get; set; }
    /// <inheritdoc cref="ICard{T}.TypeName"/>
    public string TypeName { get; set; } = nameof(TroopCard);
    /// <summary>
    /// Gets or sets the name of this card's 'parent': the <see cref="ICardSet{T}"/> that contains it.
    /// </summary>
    /// <remarks>
    /// <see cref="ICardSet{T}.MemberTypeName"/> of the parent should be equal to "TroopCard".
    /// </remarks>
    public string ParentTypeName { get; private set; } = nameof(TroopCardSet);
    /// <summary>
    /// Gets or sets the parent collection containing this <see cref="TroopCard"/> in its <see cref="ICardSet{T}.Cards"/> list.
    /// </summary>
    /// <value>An <see cref="ICardSet{T}"/> instance, if this and it have been initialized and mapped. Otherwise, <see langword="null"/>.</value>
    public ICardSet<TerrID>? CardSet { get; set; } = null;
    /// <inheritdoc cref="ICard{T}.IsTradeable"/>
    public bool IsTradeable { get; set; } = true;
    /// <inheritdoc cref="ICard{T}.Target"/>
    public TerrID[] Target { get; set; } = [];
    /// <inheritdoc cref="ITroopCard{T}.Insigne"/>
    public TroopInsignia Insigne { get; set; }
    Enum ITroopCard<TerrID>.Insigne { get => Insigne; set { Insigne = (TroopInsignia)value; } }
}
