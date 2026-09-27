using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Features.Matchmaking.Commands.JoinMatchQueue;
using NoobGg.Application.Features.Rooms.Commands.CloseRoom;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;
using Xunit;

namespace NoobGg.Application.Tests;

public class MatchmakingIntegrationTests
{
    [Fact]
    public async Task Closing_room_saves_one_session_before_removing_members()
    {
        var connectionString = Environment.GetEnvironmentVariable("MONGO_TEST_URI");
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        var databaseName = $"noobgg_test_{Guid.NewGuid():N}";
        var client = new MongoClient(connectionString);
        var mongo = new TestMongoContext(client.GetDatabase(databaseName));
        try
        {
            var room = new Room { Title = "Test squad", GameId = "game-1", CreatorId = "owner",
                Region = Region.EU, Language = Language.English, CurrentMemberCount = 2 };
            await mongo.GetCollection<Room>(CollectionNames.Rooms).InsertOneAsync(room);
            await mongo.GetCollection<RoomMember>(CollectionNames.RoomMembers).InsertManyAsync([
                new RoomMember { RoomId = room.Id, UserId = "owner", Role = RoomMemberRole.Owner },
                new RoomMember { RoomId = room.Id, UserId = "friend", Role = RoomMemberRole.Member }
            ]);
            var handler = new CloseRoomCommandHandler(mongo, new TestUser("owner"),
                new NoopRoomNotifications(), new NoopNotifications());
            var result = await handler.Handle(new CloseRoomCommand { RoomId = room.Id }, CancellationToken.None);
            Assert.True(result.IsSuccess);
            var session = await mongo.GetCollection<GameSession>(CollectionNames.GameSessions)
                .Find(s => s.RoomId == room.Id).SingleAsync();
            Assert.Equal(2, session.ParticipantIds.Count);
            Assert.Equal("owner", session.OwnerId);
            Assert.Equal(0, await mongo.GetCollection<Room>(CollectionNames.Rooms).CountDocumentsAsync(_ => true));
            Assert.Equal(0, await mongo.GetCollection<RoomMember>(CollectionNames.RoomMembers).CountDocumentsAsync(_ => true));
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task Concurrent_requests_keep_one_queue_entry_and_one_match_room()
    {
        var connectionString = Environment.GetEnvironmentVariable("MONGO_TEST_URI");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var databaseName = $"noobgg_test_{Guid.NewGuid():N}";
        var client = new MongoClient(connectionString);
        var mongo = new TestMongoContext(client.GetDatabase(databaseName));
        try
        {
            var queue = mongo.GetCollection<MatchQueueEntry>(CollectionNames.MatchQueueEntries);
            await queue.Indexes.CreateOneAsync(new CreateIndexModel<MatchQueueEntry>(
                Builders<MatchQueueEntry>.IndexKeys.Ascending(e => e.UserId),
                new CreateIndexOptions<MatchQueueEntry> { Unique = true,
                    PartialFilterExpression = Builders<MatchQueueEntry>.Filter.In(e => e.Status,
                        [MatchQueueEntryStatus.Searching, MatchQueueEntryStatus.FallbackSuggested, MatchQueueEntryStatus.Matching]) }));

            var game = new Game { Name = "Test Game", IsActive = true };
            await mongo.GetCollection<Game>(CollectionNames.Games).InsertOneAsync(game);
            await mongo.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles).InsertManyAsync([
                new UserGameProfile { UserId = "player-a", GameId = game.Id, Region = Region.EU,
                    Languages = [Language.English], EloPoints = 1500 },
                new UserGameProfile { UserId = "player-b", GameId = game.Id, Region = Region.EU,
                    Languages = [Language.English], EloPoints = 1500 }
            ]);

            var alerts = new NoopNotifications();
            var rooms = new NoopRoomNotifications();
            var playerA = new JoinMatchQueueCommandHandler(mongo, new TestUser("player-a"), rooms, alerts);
            var playerB = new JoinMatchQueueCommandHandler(mongo, new TestUser("player-b"), rooms, alerts);
            var command = new JoinMatchQueueCommand { GameId = game.Id };
            await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => playerA.Handle(command, CancellationToken.None)));

            var activeA = await queue.CountDocumentsAsync(e => e.UserId == "player-a" &&
                (e.Status == MatchQueueEntryStatus.Searching || e.Status == MatchQueueEntryStatus.FallbackSuggested ||
                 e.Status == MatchQueueEntryStatus.Matching));
            Assert.Equal(1, activeA);

            await Task.WhenAll(playerA.Handle(command, CancellationToken.None),
                playerB.Handle(command, CancellationToken.None));
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var matched = await queue.CountDocumentsAsync(e => e.Status == MatchQueueEntryStatus.Matched);
                if (matched == 2) break;
                await playerA.Handle(command, CancellationToken.None);
                await playerB.Handle(command, CancellationToken.None);
            }

            var matchedEntries = await queue.Find(e => e.Status == MatchQueueEntryStatus.Matched).ToListAsync();
            Assert.Equal(2, matchedEntries.Count);
            Assert.Single(matchedEntries.Select(e => e.MatchedRoomId).Distinct());
            Assert.Equal(1, await mongo.GetCollection<Room>(CollectionNames.Rooms).CountDocumentsAsync(_ => true));
            Assert.Equal(2, await mongo.GetCollection<RoomMember>(CollectionNames.RoomMembers).CountDocumentsAsync(_ => true));
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }

    private sealed class TestMongoContext(IMongoDatabase database) : IMongoContext
    {
        public IMongoDatabase Database => database;
        public IMongoCollection<T> GetCollection<T>(string name) => database.GetCollection<T>(name);
    }

    private sealed class TestUser(string id) : ICurrentUser
    {
        public string? UserId => id;
        public string? Username => id;
        public string? Role => "User";
        public bool IsAuthenticated => true;
    }

    private sealed class NoopRoomNotifications : IRoomNotificationService
    {
        public Task NotifyMemberJoinedAsync(string roomId, string userId, string username, CancellationToken ct = default) => Task.CompletedTask;
        public Task NotifyMemberLeftAsync(string roomId, string userId, string username, CancellationToken ct = default) => Task.CompletedTask;
        public Task NotifyRoomClosedAsync(string roomId, CancellationToken ct = default) => Task.CompletedTask;
        public Task NotifyRoomListChangedAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopNotifications : INotificationService
    {
        public Task CreateAsync(string userId, NotificationType type, string title, string body,
            Dictionary<string, string>? data = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendBlockListChangedAsync(string userId1, string userId2, bool isBlocked, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendFriendListChangedAsync(string userId1, string userId2, CancellationToken ct = default) => Task.CompletedTask;
        public Task ForceDisconnectAsync(string userId, string reason, CancellationToken ct = default) => Task.CompletedTask;
    }
}
