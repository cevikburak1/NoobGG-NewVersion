namespace NoobGg.Domain.Entities;

public class MatchResult : BaseEntity
{
    public string GameId { get; set; } = string.Empty;
    public string Player1Id { get; set; } = string.Empty;
    public string Player2Id { get; set; } = string.Empty;
    public string WinnerId { get; set; } = string.Empty;
    public int Player1EloBefore { get; set; }
    public int Player2EloBefore { get; set; }
    public int Player1EloChange { get; set; }
    public int Player2EloChange { get; set; }
}
