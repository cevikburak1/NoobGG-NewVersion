using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Rooms.DTOs;
using NoobGg.Domain.Enums;
using NoobGg.Domain.ValueObjects;

namespace NoobGg.Application.Features.Rooms.Commands.CreateRoom;

public record CreateRoomCommand : IRequest<Result<RoomDetailResponse>>
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string GameId { get; init; } = string.Empty;
    public bool IsPublic { get; init; } = true;
    public Region Region { get; init; }
    public Language Language { get; init; }
    public List<string> Tags { get; init; } = [];
    public RankRange? RankRange { get; init; }
}
