using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Backend.Data;
using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Backend.Tests.IntegrationTests.Common
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        private string _dbName = $"TestDb_{Guid.NewGuid()}";
        private bool _disposed = false;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for SQL Server
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                    options.UseInternalServiceProvider(null); // Prevent service provider issues
                });

                // Remove existing MongoDB registration
                var mongoDescriptors = services.Where(
                    d => d.ServiceType == typeof(IMongoDatabase) ||
                         d.ServiceType == typeof(IMongoClient) ||
                         d.ServiceType == typeof(MongoDbContext))
                    .ToList();

                foreach (var mongoDescriptor in mongoDescriptors)
                {
                    services.Remove(mongoDescriptor);
                }

                // Mock MongoDB for testing (instead of real connection)
                var mockMongoClient = new Mock<IMongoClient>();
                var mockMongoDatabase = new Mock<IMongoDatabase>();
                var mockMongoCollection = new Mock<IMongoCollection<Backend.Models.Nosql.Form>>();

                mockMongoClient.Setup(x => x.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
                    .Returns(mockMongoDatabase.Object);

                services.AddSingleton<IMongoClient>(mockMongoClient.Object);
                services.AddSingleton<IMongoDatabase>(mockMongoDatabase.Object);

                // Configure MongoDbSettings for test
                services.Configure<MongoDbSettings>(options =>
                {
                    options.ConnectionString = "mongodb://localhost:27017";
                    options.DatabaseName = $"TestFormBuilder_{Guid.NewGuid()}";
                });

                // Mock MongoDbContext
                services.AddScoped<MongoDbContext>(sp =>
                {
                    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>();
                    var mockContext = new Mock<MongoDbContext>(settings);
                    mockContext.Setup(x => x.Forms).Returns(mockMongoCollection.Object);
                    return mockContext.Object;
                });

                // Build the service provider
                var serviceProvider = services.BuildServiceProvider();

                // Create the database and ensure it's ready
                using (var scope = serviceProvider.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                    var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

                    try
                    {
                        db.Database.EnsureCreated();
                        // Optionally seed test data here
                        SeedTestData(db);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred seeding the database with test data.");
                    }
                }
            });

            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Override configuration for testing
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("JwtSettings:SecretKey", "ThisIsAVerySecretKeyForTestingPurposesOnly123456"),
                    new KeyValuePair<string, string>("JwtSettings:Issuer", "TestIssuer"),
                    new KeyValuePair<string, string>("JwtSettings:Audience", "TestAudience"),
                    new KeyValuePair<string, string>("Logging:LogLevel:Default", "Warning"),
                    new KeyValuePair<string, string>("Logging:LogLevel:Microsoft.AspNetCore", "Warning"),
                });
            });
        }

        private void SeedTestData(ApplicationDbContext context)
        {
            // Add any test seed data here if needed
            // For auth tests, we typically start with an empty database
            context.SaveChanges();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _disposed = true;
                
                try
                {
                    // Try to clean up the in-memory database if possible
                    // In-memory database is automatically cleaned up when disposed
                    // No need to manually delete it
                }
                catch
                {
                    // Ignore any disposal errors
                }
            }

            base.Dispose(disposing);
        }
    }
}
