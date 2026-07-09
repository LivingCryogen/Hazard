namespace HazardBackend.DTOs;

public record MoveActionDto(
     int ActionId,
     int Player,
     string SourceTerritory,
     string TargetTerritory,
     bool MaxAdvanced
) : BaseDto;
