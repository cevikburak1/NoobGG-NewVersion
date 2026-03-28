namespace NoobGg.Application.Features.Blocks.DTOs;

public record BlockedUserResponse(
    string BlockId,
    string UserId,
    string Username,
    string? AvatarUrl,
    DateTime BlockedAt);
