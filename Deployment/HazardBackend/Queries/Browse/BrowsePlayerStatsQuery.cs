namespace HazardBackend.Queries.Browse;

public sealed class BrowsePlayerStatsQuery : BrowseQuery
{
    public enum FilterProperty
    {
        IsDemo,
        InstallId,
        PlayerName,
        GamesStarted,
        GamesCompleted,
        GamesWon,
        FirstGameStarted,
        FirstGameCompleted,
        LastGameStarted,
        LastGameCompleted,
        TotalGamesDuration,
        AttacksWon,
        AttacksLost,
        AttacksTied,
        Conquests,
        Retreats,
        ForcedRetreats,
        AttackDiceRolled,
        DefenseDiceRolled,
        Moves,
        MaxAdvances,
        TradeIns,
        TotalOccupationBonus
    }
    public enum SortProperty
    {
        IsDemo,
        InstallId,
        PlayerName,
        GamesStarted,
        GamesCompleted,
        GamesWon,
        FirstGameStarted,
        FirstGameCompleted,
        LastGameStarted,
        LastGameCompleted,
        TotalGamesDuration,
        AttacksWon,
        AttacksLost,
        AttacksTied,
        Conquests,
        Retreats,
        ContinentsClaimed,
        ContinentsConquered,
        ContinentsLost,
        ContinentsReacquired,
        ForcedRetreats,
        AttackDiceRolled,
        DefenseDiceRolled,
        Moves,
        MaxAdvances,
        TradeIns,
        TotalOccupationBonus
    }

    public Sort<SortProperty>? SortDescriptor { get; init; }
    public List<Filter<FilterProperty>> Filters { get; init; } = [];
}