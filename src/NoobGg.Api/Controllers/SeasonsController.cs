using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Api.Controllers;

[Authorize]
[Route("api/seasons")]
public class SeasonsController(IMongoContext mongo) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? gameId, CancellationToken ct)
    {
        var seasons = mongo.GetCollection<Season>(CollectionNames.Seasons);
        var filter = gameId is null
            ? Builders<Season>.Filter.Empty
            : Builders<Season>.Filter.Eq(s => s.GameId, gameId);
        return Ok(await seasons.Find(filter).SortByDescending(s => s.StartsAt).Limit(50).ToListAsync(ct));
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateSeasonRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100 ||
            request.StartsAt >= request.EndsAt || request.EndsAt <= DateTime.UtcNow)
            return BadRequest("Invalid season name or dates");
        var games = mongo.GetCollection<Game>(CollectionNames.Games);
        if (!await games.Find(g => g.Id == request.GameId && g.IsActive).AnyAsync(ct))
            return BadRequest("Game is unavailable");
        var season = new Season
        {
            Name = request.Name.Trim(), GameId = request.GameId,
            StartsAt = request.StartsAt, EndsAt = request.EndsAt
        };
        await mongo.GetCollection<Season>(CollectionNames.Seasons).InsertOneAsync(season, cancellationToken: ct);
        return Created($"/api/seasons/{season.Id}", season);
    }

    [HttpGet("{id}/standings")]
    public async Task<IActionResult> Standings(string id, CancellationToken ct)
    {
        var season = await mongo.GetCollection<Season>(CollectionNames.Seasons)
            .Find(s => s.Id == id).FirstOrDefaultAsync(ct);
        if (season is null) return NotFound();
        var tournaments = await mongo.GetCollection<Tournament>(CollectionNames.Tournaments)
            .Find(t => t.SeasonId == id && t.Status == TournamentStatus.Completed)
            .Project(t => t.Id).ToListAsync(ct);
        var entries = await mongo.GetCollection<TournamentEntry>(CollectionNames.TournamentEntries)
            .Find(Builders<TournamentEntry>.Filter.In(e => e.TournamentId, tournaments))
            .ToListAsync(ct);
        var standings = entries.GroupBy(e => e.ParticipantId)
            .Select(group => new
            {
                UserId = group.Key,
                Wins = group.Count(e => e.Placement == 1),
                Podiums = group.Count(e => e.Placement is 1 or 2),
                Points = group.Sum(e => e.Placement == 1 ? 3 : e.Placement == 2 ? 1 : 0)
            })
            .OrderByDescending(row => row.Points).ThenByDescending(row => row.Wins)
            .Take(100).ToList();
        var userIds = standings.Select(s => s.UserId).ToList();
        var users = await mongo.GetCollection<User>(CollectionNames.Users)
            .Find(Builders<User>.Filter.In(u => u.Id, userIds)).ToListAsync(ct);
        var names = users.ToDictionary(u => u.Id, u => u.Username);
        return Ok(new { season, standings = standings.Select(s => new
        {
            s.UserId, Username = names.GetValueOrDefault(s.UserId, "Unknown"), s.Wins, s.Podiums, s.Points
        }) });
    }
}

public record CreateSeasonRequest(string Name, string GameId, DateTime StartsAt, DateTime EndsAt);
