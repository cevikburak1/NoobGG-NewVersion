using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Auth.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Auth.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetCurrentUserQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<UserResponse>> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<UserResponse>.Unauthorized();

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var user = await users.Find(Builders<User>.Filter.Eq(u => u.Id, _currentUser.UserId)).FirstOrDefaultAsync(ct);

        if (user is null)
            return Result<UserResponse>.NotFound("User not found");

        return Result<UserResponse>.Success(new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            Role = user.Role.ToString(),
            IsEmailVerified = user.IsEmailVerified,
            IsProfileComplete = user.IsProfileComplete,
            CreatedAt = user.CreatedAt
        });
    }
}
