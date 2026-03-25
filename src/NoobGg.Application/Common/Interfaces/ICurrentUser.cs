namespace NoobGg.Application.Common.Interfaces;

public interface ICurrentUser
{
    string? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
