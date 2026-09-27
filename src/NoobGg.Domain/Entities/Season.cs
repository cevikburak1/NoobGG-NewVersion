namespace NoobGg.Domain.Entities;

public class Season : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
}
