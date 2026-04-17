namespace NoobGg.Domain.Entities;

public class GuildEvent : BaseEntity
{
    public string GuildId { get; set; } = string.Empty;
    public string CreatorId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string? GameId { get; set; }
    public string? TournamentId { get; set; }
}
