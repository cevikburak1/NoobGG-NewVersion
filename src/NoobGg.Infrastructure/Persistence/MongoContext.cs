using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using NoobGg.Application.Common.Interfaces;

namespace NoobGg.Infrastructure.Persistence;

public class MongoContext : IMongoContext
{
    private static bool _conventionsRegistered;
    private static readonly object Lock = new();
    private readonly IMongoDatabase _database;

    public MongoContext(IOptions<MongoDbSettings> settings)
    {
        RegisterConventions();
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    private static void RegisterConventions()
    {
        if (_conventionsRegistered) return;
        lock (Lock)
        {
            if (_conventionsRegistered) return;
            var pack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
            ConventionRegistry.Register("IgnoreExtraElements", pack, _ => true);
            _conventionsRegistered = true;
        }
    }

    public IMongoDatabase Database => _database;

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        return _database.GetCollection<T>(name);
    }
}
