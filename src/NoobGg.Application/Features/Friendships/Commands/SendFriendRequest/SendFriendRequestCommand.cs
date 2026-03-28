using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Friendships.Commands.SendFriendRequest;

public class SendFriendRequestCommand : IRequest<Result>
{
    public string AddresseeId { get; set; } = string.Empty;
}
