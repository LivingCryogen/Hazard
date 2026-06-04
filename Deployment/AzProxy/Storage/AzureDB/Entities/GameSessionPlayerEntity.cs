namespace HazardBackend.Storage.AzureDB.Entities;

public class GameSessionPlayerEntity
{
    // Composite Key
    public Guid GameId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public Guid InstallId { get; set; }

    // Data
    public bool IsDemo { get; set; }
}
