using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class TournamentMatch : BaseEntity
{
    public string TournamentId { get; set; } = string.Empty;
    public int Round { get; set; }
    public int MatchNumber { get; set; }
    public string? Participant1Id { get; set; }
    public string? Participant2Id { get; set; }
    public string? WinnerId { get; set; }
    public TournamentMatchStatus Status { get; set; } = TournamentMatchStatus.Pending;
    public string? NextMatchId { get; set; }
    public string? LoserNextMatchId { get; set; }
}
