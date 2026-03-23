namespace HazardBackend.Storage.AzureDB.Services.Queries;

public enum QueryType : int
{
    None = 0,
    Leaderboard = 1,
    GameSession = 2,
    PlayerStats = 3,
    Action = 4
}
