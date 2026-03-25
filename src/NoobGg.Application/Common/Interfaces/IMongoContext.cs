using MongoDB.Driver;

namespace NoobGg.Application.Common.Interfaces;

public interface IMongoContext
{
    IMongoCollection<T> GetCollection<T>(string name);
    IMongoDatabase Database { get; }
}
