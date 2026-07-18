namespace HazardBackend.Storage.AzureDB.Entities;

public class ClaimActionEntity
{
    // Key
    public Guid GameId { get; set; }
    public int ActionId { get; set; }

    // Foreign composite key to PlayerIdentityEntity
    public string PlayerName { get; set; } = string.Empty;
    public Guid InstallID { get; set; }

    // Data
    public bool IsDemo { get; set; } = false;
    public string ClaimedTerritory { get; set; } = string.Empty;

    public GameSessionEntity GameSession { get; set; } = null!; // Navigation property for EF
}
