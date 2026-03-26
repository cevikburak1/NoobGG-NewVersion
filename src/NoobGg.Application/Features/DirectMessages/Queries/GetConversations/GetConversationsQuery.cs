using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.DirectMessages.DTOs;

namespace NoobGg.Application.Features.DirectMessages.Queries.GetConversations;

public record GetConversationsQuery : IRequest<Result<List<ConversationResponse>>>;
