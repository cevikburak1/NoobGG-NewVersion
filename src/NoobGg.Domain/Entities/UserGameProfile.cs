using NoobGg.Domain.Enums;
using NoobGg.Domain.ValueObjects;

namespace NoobGg.Domain.Entities;

public class UserGameProfile : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string Rank { get; set; } = string.Empty;
    public string? Role { get; set; }
    public Region Region { get; set; }
    public List<Language> Languages { get; set; } = [];
    public ExperienceLevel ExperienceLevel { get; set; }
    public CommunicationPreference CommunicationPreference { get; set; }
    public int? HoursPlayed { get; set; }
    public bool LookingForTeam { get; set; }
    public string? Note { get; set; }
    public int EloPoints { get; set; } = 1500;
    public RankTier RankTier { get; set; } = RankTier.Gold;
    public List<EloSnapshot> EloHistory { get; set; } = [];
}
