namespace NoobGg.Application.Features.Tournaments.DTOs;

public record TournamentListItemResponse(
    string Id, string Name, string? Description, string GameId, string GameName,
    string OrganizerId, string OrganizerUsername, string? GuildId,
    string Format, string Status, int MaxParticipants, int CurrentParticipants,
    DateTime RegistrationDeadline, DateTime? StartsAt, List<string> PrizeBadges, DateTime CreatedAt);

public record TournamentDetailResponse(
    string Id, string Name, string? Description, string GameId, string GameName,
    string OrganizerId, string OrganizerUsername, string? GuildId,
    string Format, string Status, int MaxParticipants, int CurrentParticipants,
    DateTime RegistrationDeadline, DateTime? StartsAt,
    int CurrentRound, int TotalRounds, List<string> PrizeBadges,
    List<TournamentEntryResponse> Entries, List<TournamentMatchResponse> Matches,
    bool IsParticipant, DateTime CreatedAt);

public record TournamentEntryResponse(
    string Id, string ParticipantId, string ParticipantName, string EntryType,
    string? GuildId, int Seed, bool IsEliminated, int Placement, List<string> EarnedBadges);

public record TournamentMatchResponse(
    string Id, int Round, int MatchNumber,
    string? Participant1Id, string? Participant1Name,
    string? Participant2Id, string? Participant2Name,
    string? WinnerId, string Status, string? NextMatchId);

public record TournamentListResponse(List<TournamentListItemResponse> Tournaments, int TotalCount, bool HasMore);
