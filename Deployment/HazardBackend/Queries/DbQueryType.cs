namespace HazardBackend.Queries;

public enum DbQueryType
{
    Browse,    // sorted and/or filtered GameSessions and/or PlayerStats by properties
    Lookup     // specific GameSession or PlayerStats by unique identifier
    // Analytics  // aggregate data for analytics purposes (e.g., average games won per player); Not yet implemented
}

