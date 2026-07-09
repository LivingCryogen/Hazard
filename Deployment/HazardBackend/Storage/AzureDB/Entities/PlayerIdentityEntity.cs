namespace HazardBackend.Storage.AzureDB.Entities;

public class PlayerIdentityEntity
{
    // Composite Key
    public Guid InstallId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Data
    public bool IsDemo { get; set; }

    // Navigation property for EF
    public PlayerStatsEntity? PlayerStats { get; set; }
}
