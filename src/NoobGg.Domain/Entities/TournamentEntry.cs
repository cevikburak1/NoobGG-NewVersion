using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class TournamentEntry : BaseEntity
{
    public string TournamentId { get; set; } = string.Empty;
    public string ParticipantId { get; set; } = string.Empty;
    public TournamentEntryType EntryType { get; set; } = TournamentEntryType.Player;
    public string? GuildId { get; set; }
    public int Seed { get; set; }
    public bool IsEliminated { get; set; }
    public int Placement { get; set; }
    public List<string> EarnedBadges { get; set; } = [];
}
