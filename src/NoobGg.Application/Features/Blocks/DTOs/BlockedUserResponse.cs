namespace NoobGg.Application.Features.Blocks.DTOs;

public record BlockedUserResponse(
    string BlockId,
    string UserId,
    string Username,
    DateTime BlockedAt);
