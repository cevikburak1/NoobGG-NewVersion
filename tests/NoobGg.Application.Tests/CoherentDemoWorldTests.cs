using NoobGg.Application.Common.Constants;
using NoobGg.Application.Demo;
using NoobGg.Domain.Entities;
using Xunit;

namespace NoobGg.Application.Tests;

public class CoherentDemoWorldTests
{
    private static readonly DateTime Epoch = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
    private static CoherentDemoWorld World() => CoherentDemoWorld.Build(Epoch, "test-only-hash", PlanDefaults.All.Select((p, i) => new SubscriptionPlan { Id = $"plan-{i}", Tier = p.Tier }).ToList());

    [Fact]
    public void Entire_world_has_valid_prerequisites_and_every_user_has_activity()
    {
        var world = World();
        Assert.Empty(world.Validate());
        Assert.Equal(200, world.Rows<User>(CollectionNames.Users).Count);
        Assert.Equal(400, world.Rows<UserGameProfile>(CollectionNames.UserGameProfiles).Count);
        Assert.Equal(50, world.Rows<Room>(CollectionNames.Rooms).Count);
        Assert.Equal(10, world.Rows<Guild>(CollectionNames.Guilds).Count);
    }

    [Fact]
    public void IDs_are_reproducible_and_input_plans_are_not_mutated()
    {
        var one = World(); var two = World();
        Assert.Equal(one.Collections.SelectMany(c => c.Value).Select(x => x.Id), two.Collections.SelectMany(c => c.Value).Select(x => x.Id));
    }

    [Theory]
    [InlineData("capacity")]
    [InlineData("dm-unread")]
    [InlineData("rsvp-non-member")]
    [InlineData("winner-non-participant")]
    [InlineData("comment-counter")]
    public void Validator_detects_broken_relationships(string fault)
    {
        var world = World();
        switch (fault)
        {
            case "capacity": world.Rows<Room>(CollectionNames.Rooms)[0].CurrentMemberCount = 8; break;
            case "dm-unread": world.Rows<Conversation>(CollectionNames.Conversations)[0].Participant1UnreadCount = 88; break;
            case "rsvp-non-member": world.Rows<GuildEvent>(CollectionNames.GuildEvents)[0].GoingUserIds.Add("missing-user"); break;
            case "winner-non-participant": world.Rows<TournamentMatch>(CollectionNames.TournamentMatches)[0].WinnerId = "missing-user"; break;
            case "comment-counter": world.Rows<CommunityPost>(CollectionNames.CommunityPosts)[0].CommentCount = 900; break;
        }
        Assert.NotEmpty(world.Validate());
    }
}
