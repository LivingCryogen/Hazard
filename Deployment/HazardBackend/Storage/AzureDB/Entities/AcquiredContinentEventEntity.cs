namespace HazardBackend.Storage.AzureDB.Entities;

public class AcquiredContinentEventEntity
{
    // Key
    public Guid GameId { get; set; }
    public int FromActionId { get; set; }

    // Data
    public bool IsDemo { get; set; } = false;
    public string Continent { get; set; } = string.Empty;
    public string? PrevOwner { get; set; } = null;  
    public bool FromClaim { get; set; } = false;

    // Foreign composite key to PlayerIdentityEntity
    public string NewOwner { get; set; } = string.Empty;
    public Guid InstallId { get; set; }

    public GameSessionEntity GameSession { get; set; } = null!; // Navigation property for EF
}
