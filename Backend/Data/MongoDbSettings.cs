namespace Backend.Data;

// This class matches the structure in appsettings.json
public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}