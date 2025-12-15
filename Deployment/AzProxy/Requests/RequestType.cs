namespace AzProxy.Requests;

public enum RequestType : int
{
    None = 0,
    Verify = 1,
    GenSAS = 2,
    Sync = 3,
    Leaderboard = 4,
    Search = 5,
    Prune = 6
}
