namespace HazardBackend.DTOs;

public class AcquiredContinentEventDto
{
    public string Continent { get; set; } = string.Empty;
    public int FromActionId { get; set; }
    public int PrevOwner { get; set; }
    public int NewOwner { get; set; }
}
