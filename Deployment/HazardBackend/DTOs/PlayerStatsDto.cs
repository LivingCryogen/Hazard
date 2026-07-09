namespace HazardBackend.DTOs;

public record PlayerStatsDto(
    Guid InstallId,
    string Name,
    bool IsDemo,
    int GamesStarted,
    int GamesCompleted,
    int GamesWon,
    DateTime FirstGameStarted,
    DateTime? FirstGameCompleted,
    DateTime LastGameStarted,
    DateTime? LastGameCompleted,
    TimeSpan TotalGamesDuration,
    int AttacksWon,
    int AttacksLost,
    int AttacksTied,
    int Conquests,
    int Retreats,
    int ForcedRetreats,
    int AttackDiceRolled,
    int DefenseDiceRolled,
    int Moves,
    int MaxAdvances,
    int TradeIns,
    int TotalOccupationBonus
) : BaseDto;
