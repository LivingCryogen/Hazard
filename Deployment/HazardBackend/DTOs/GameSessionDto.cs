using HazardBackend.Storage.AzureDB.Entities;

namespace HazardBackend.DTOs;

public record GameSessionDto(
        int Version,
        Guid Id,
        Guid InstallId,
        DateTime StartTime,
        DateTime? EndTime,
        int? Winner,
        int NumActions,
        List<ClaimActionDto> Claims,
        List<AttackActionDto> Attacks,
        List<MoveActionDto> Moves,
        List<TradeActionDto> Trades,
        List<AcquiredContinentEventDto> AcquiredContinents,
        Dictionary<string, string> PlayerNumsAndNames
    ) : BaseDto;
