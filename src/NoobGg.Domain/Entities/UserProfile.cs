using NoobGg.Domain.ValueObjects;

namespace NoobGg.Domain.Entities;

public class UserProfile : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? Bio { get; set; }
    public string? Country { get; set; }
    public string? Timezone { get; set; }
    public Availability? Availability { get; set; }
}
