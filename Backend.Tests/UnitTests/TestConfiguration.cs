using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Backend.Tests.UnitTests
{
    public static class TestConfiguration
    {
        public static IConfiguration GetConfiguration()
        {
            var configData = new Dictionary<string, string>
            {
                {"JwtSettings:SecretKey", "ThisIsAVerySecretKeyForTestingPurposesOnly123456"},
                {"JwtSettings:Issuer", "TestIssuer"},
                {"JwtSettings:Audience", "TestAudience"},
                {"ConnectionStrings:SqlServerConnection", "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;"},
                {"MongoDbSettings:ConnectionString", "mongodb://localhost:27017"},
                {"MongoDbSettings:DatabaseName", "TestFormBuilder"}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();
        }
    }
}