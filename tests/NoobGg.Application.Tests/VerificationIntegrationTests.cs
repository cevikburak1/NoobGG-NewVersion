using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Features.Auth.Commands.ResendVerificationEmail;
using NoobGg.Application.Features.Auth.Commands.VerifyEmail;
using NoobGg.Domain.Entities;
using Xunit;

namespace NoobGg.Application.Tests;

public class VerificationIntegrationTests
{
    [Fact]
    public async Task Resent_code_can_verify_account_and_old_code_is_invalidated()
    {
        var uri = Environment.GetEnvironmentVariable("MONGO_TEST_URI");
        Assert.False(string.IsNullOrWhiteSpace(uri), "Set MONGO_TEST_URI to an isolated test MongoDB");
        var client = new MongoClient(uri);
        var db = $"noobgg_test_{Guid.NewGuid():N}";
        var mongo = new TestMongo(client.GetDatabase(db));
        try
        {
            var user = new User { Username = "verify_test", Email = "verify@example.test" };
            await mongo.GetCollection<User>(CollectionNames.Users).InsertOneAsync(user);
            var old = new EmailVerificationToken { UserId = user.Id, Token = "123456", ExpiresAt = DateTime.UtcNow.AddHours(1) };
            var tokens = mongo.GetCollection<EmailVerificationToken>(CollectionNames.EmailVerificationTokens);
            await tokens.InsertOneAsync(old);
            var email = new CapturingEmail();
            var resent = await new ResendVerificationEmailCommandHandler(mongo, email)
                .Handle(new ResendVerificationEmailCommand { Email = user.Email }, CancellationToken.None);
            Assert.True(resent.IsSuccess);
            Assert.Matches("^[0-9]{6}$", email.Code!);
            Assert.True((await tokens.Find(t => t.Id == old.Id).SingleAsync()).IsUsed);
            var verified = await new VerifyEmailCommandHandler(mongo, new TestJwt())
                .Handle(new VerifyEmailCommand { Email = user.Email, Code = email.Code! }, CancellationToken.None);
            Assert.True(verified.IsSuccess, verified.Error);
            Assert.True(verified.Data!.User.IsEmailVerified);
        }
        finally { await client.DropDatabaseAsync(db); }
    }

    private sealed class TestMongo(IMongoDatabase database) : IMongoContext
    {
        public IMongoDatabase Database => database;
        public IMongoCollection<T> GetCollection<T>(string name) => database.GetCollection<T>(name);
    }
    private sealed class CapturingEmail : IEmailService
    {
        public string? Code { get; private set; }
        public Task SendVerificationEmailAsync(string toEmail, string username, string verificationToken, CancellationToken ct = default)
        { Code = verificationToken; return Task.CompletedTask; }
        public Task SendPasswordResetEmailAsync(string toEmail, string username, string resetToken, CancellationToken ct = default) => Task.CompletedTask;
    }
    private sealed class TestJwt : IJwtTokenService
    {
        public int AccessTokenExpirationMinutes => 15;
        public int RefreshTokenExpirationDays => 7;
        public string GenerateAccessToken(string userId, string username, string role) => "test-access";
        public string GenerateRefreshToken() => Guid.NewGuid().ToString();
        public (string UserId, string Username, string Role)? ValidateToken(string token) => null;
    }
}
