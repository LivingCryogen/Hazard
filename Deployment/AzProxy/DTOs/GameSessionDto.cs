namespace HazardBackend.DTOs;

public record GameSessionDto(
        int Version,
        Guid Id,
        Guid InstallId,
        DateTime StartTime,
        DateTime? EndTime,
        int? Winner,
        int NumActions,
        List<AttackActionDto> Attacks,
        List<MoveActionDto> Moves,
        List<TradeActionDto> Trades,
        Dictionary<string, string> PlayerNumsAndNames
    ) : BaseDto;
