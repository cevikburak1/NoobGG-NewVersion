namespace NoobGg.Domain.Entities;

public class Block : BaseEntity
{
    public string BlockerId { get; set; } = string.Empty;
    public string BlockedUserId { get; set; } = string.Empty;
}
