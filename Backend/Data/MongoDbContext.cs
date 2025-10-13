using Backend.Data;
using Backend.Models.Nosql;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Backend.Models.Nosql;

namespace Backend.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoDbSettings _settings;

    // Constructor receives settings from DI container
    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        _settings = settings.Value;

        // Create MongoDB client using connection string from settings
        var client = new MongoClient(_settings.ConnectionString);

        // Get database using database name from settings
        _database = client.GetDatabase(_settings.DatabaseName);
    }

    // Collection accessors
    public IMongoCollection<Form> Forms =>
        _database.GetCollection<Form>("forms");

    // Add more collections as needed
    // public IMongoCollection<AnotherModel> AnotherCollection => 
    //     _database.GetCollection<AnotherModel>("collectionName");

    // Helper method to get any collection
    public IMongoCollection<T> GetCollection<T>(string collectionName) =>
        _database.GetCollection<T>(collectionName);

    // Property to access database directly if needed
    public IMongoDatabase Database => _database;

    // Property to access settings if needed
    public MongoDbSettings Settings => _settings;
}