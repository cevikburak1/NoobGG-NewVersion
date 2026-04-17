using NoobGg.Application.Features.Matchmaking.Commands.JoinMatchQueue;
using Xunit;

namespace NoobGg.Application.Tests;

public class JoinMatchQueueCommandValidatorTests
{
    private readonly JoinMatchQueueCommandValidator _validator = new();

    [Fact]
    public void Empty_game_id_is_invalid()
    {
        var result = _validator.Validate(new JoinMatchQueueCommand { GameId = "" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Non_empty_game_id_is_valid()
    {
        var result = _validator.Validate(new JoinMatchQueueCommand { GameId = "game-1" });
        Assert.True(result.IsValid);
    }
}
