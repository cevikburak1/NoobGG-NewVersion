namespace NoobGg.Domain.Entities;

public class Favorite : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string FavoriteUserId { get; set; } = string.Empty;
}
