using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using NoobGg.Application.Common.Interfaces;
using StackExchange.Redis;

namespace NoobGg.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(IMongoContext mongo, IConnectionMultiplexer redis) : ControllerBase
{
    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            await Task.WhenAll(
                mongo.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: deadline.Token),
                redis.GetDatabase().PingAsync().WaitAsync(deadline.Token));
            return Ok(new { status = "ready" });
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // Do not expose connection strings or provider exception messages.
            return StatusCode(503, new { status = "not_ready" });
        }
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}
