namespace NoobGg.Domain.Enums;

public enum MatchQueueEntryStatus
{
    Searching = 0,
    Matched = 1,
    FallbackSuggested = 2,
    Cancelled = 3,
    Expired = 4
}
