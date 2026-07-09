namespace HazardBackend.DTOs;

public record AttackActionDto(
     int ActionId,
     int Player,
     string SourceTerritory,
     string TargetTerritory,
     int Defender,
     int AttackerInitialArmies,
     int DefenderInitialArmies,
     int AttackerDice,
     int DefenderDice,
     int AttackerLoss,
     int DefenderLoss,
     bool Retreated,
     bool Conquered
) : BaseDto;
