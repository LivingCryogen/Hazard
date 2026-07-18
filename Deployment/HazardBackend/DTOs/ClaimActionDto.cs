using Microsoft.Identity.Client;

namespace HazardBackend.DTOs;

public record ClaimActionDto : BaseDto
{
    public Guid GameId { get; set; }
    public int ActionId { get; set; }
    public int Player { get; set; }
    public Guid InstallId { get; set; }

    public string ClaimedTerritory { get; set; } = string.Empty;
}
