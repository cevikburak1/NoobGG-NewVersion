using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class Tournament : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string GameId { get; set; } = string.Empty;
    public string OrganizerId { get; set; } = string.Empty;
    public string? GuildId { get; set; }
    public TournamentFormat Format { get; set; } = TournamentFormat.SingleElimination;
    public TournamentStatus Status { get; set; } = TournamentStatus.Registration;
    public int MaxParticipants { get; set; } = 16;
    public int CurrentParticipants { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public DateTime? StartsAt { get; set; }
    public int CurrentRound { get; set; }
    public int TotalRounds { get; set; }
    public List<string> PrizeBadges { get; set; } = [];
}
