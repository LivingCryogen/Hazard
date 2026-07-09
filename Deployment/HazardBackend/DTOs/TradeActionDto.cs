namespace HazardBackend.DTOs;

public record TradeActionDto(
    int ActionId,
    int Player,
    string[] CardTargets,
    int TradeValue,
    int OccupiedBonus): BaseDto;
