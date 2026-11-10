using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Demo;
using NoobGg.Domain.Entities;

namespace NoobGg.Api.Controllers;

[ApiController]
[Route("api/demo")]
public sealed class DemoController(IWebHostEnvironment environment, IConfiguration config, IMongoContext mongo) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        if (!environment.IsDevelopment() || config["DemoSeed:Scenario"] != "coherent-200") return NotFound();
        var marker = await mongo.Database.GetCollection<BsonDocument>(CollectionNames.DemoSeedMarkers)
            .Find(Builders<BsonDocument>.Filter.Eq("_id", CoherentDemoWorld.Version)).FirstOrDefaultAsync(ct);
        if (marker == null) return NotFound();
        return Ok(new { scenario = CoherentDemoWorld.Version, synthetic = true, state = marker["State"].AsString,
            users = await mongo.GetCollection<User>(CollectionNames.Users).CountDocumentsAsync(_ => true, cancellationToken: ct),
            countsAtSeed = marker.GetValue("Counts", new BsonDocument()).AsBsonDocument.Elements.ToDictionary(e => e.Name, e => e.Value.AsInt32),
            validationErrorsAtSeed = marker.GetValue("ValidationErrors", -1).AsInt32,
            accounts = new { user = "Player_0006", guildOwner = "Player_0000", moderator = "Player_0002", admin = "Player_0001" } });
    }
}
