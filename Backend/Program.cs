using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Backend.Data;
using Backend.Models.Sql;
using Backend.Models.Nosql;
using System.Text;
using Backend.Business;
using Backend.DataAccess;
using Backend.Enums;
using Backend.Filters;
using Backend.Utils;

var builder = WebApplication.CreateBuilder(args);

// =====================================
// 1. CONFIGURE SQL SERVER
// =====================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SqlServerConnection");

    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(30);
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// =====================================
// 2. CONFIGURE MONGODB
// =====================================
// Register MongoDB settings from appsettings.json
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

// Register MongoDB client as singleton
builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
    return new MongoClient(settings!.ConnectionString);
});

// Register MongoDB database
builder.Services.AddScoped(serviceProvider =>
{
    var settings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
    var client = serviceProvider.GetRequiredService<IMongoClient>();
    return client.GetDatabase(settings!.DatabaseName);
});

// Register MongoDbContext
builder.Services.AddScoped<MongoDbContext>();

// =====================================
// 3. ADD CONTROLLERS & API SERVICES
// =====================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// =====================================
// 4. CONFIGURE CORS
// =====================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });

    options.AddPolicy("AllowSpecificOrigins",
        policy =>
        {
            policy.WithOrigins(
                    "http://localhost:3000",
                    "http://localhost:5173",
                    "http://localhost:4200",
                    "http://localhost:22660"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// =====================================
// 5. CONFIGURE SWAGGER
// =====================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FormBuilder API",
        Version = "v1",
        Description = "API for FormBuilder application with SQL Server and MongoDB"
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// =====================================
// 6. ADD AUTHENTICATION (Optional - for later)
// =====================================
// Uncomment when you're ready to add JWT authentication

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]))
        };
        
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Read token from cookie
                context.Token = context.Request.Cookies["jwt"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();


// =====================================
// 7. ADD OTHER SERVICES
// =====================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

builder.Services.AddScoped<IUserDAL, UserDAL>();
builder.Services.AddScoped<IFormDAL, FormDAL>();
builder.Services.AddScoped<IResponseDAL, ResponseDAL>();
builder.Services.AddScoped<IAuthBL, AuthBL>();
builder.Services.AddScoped<IFormBL, FormBL>();
builder.Services.AddScoped<IResponseBL, ResponseBL>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenHelper, JwtTokenHelper>();

builder.Services.AddScoped<UserContextActionFilter>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// =====================================
// BUILD THE APPLICATION
// =====================================
var app = builder.Build();

// =====================================
// TEST DATABASE CONNECTIONS ON STARTUP
// =====================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        // Test SQL Server Connection
        logger.LogInformation("Testing SQL Server connection...");
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        // Create database if it doesn't exist
        var created = await dbContext.Database.EnsureCreatedAsync();
        if (created)
        {
            logger.LogInformation("✅ SQL Server database created!");
        }

        var canConnect = await dbContext.Database.CanConnectAsync();
        if (canConnect)
        {
            logger.LogInformation("✅ SQL Server connected successfully!");
        }

        // Test MongoDB Connection
        logger.LogInformation("Testing MongoDB connection...");
        var mongoContext = services.GetRequiredService<MongoDbContext>();

        // Try to ping MongoDB
        var database = services.GetRequiredService<IMongoDatabase>();
        var ping = await database.RunCommandAsync<MongoDB.Bson.BsonDocument>(
            new MongoDB.Bson.BsonDocument("ping", 1));

        if (ping != null)
        {
            logger.LogInformation("✅ MongoDB connected successfully!");
        }

        // Optional: Seed initial data
        await SeedInitialData(dbContext, mongoContext, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ An error occurred while connecting to databases!");
        // Don't throw - allow app to start even if DB is down
    }
}

// =====================================
// CONFIGURE HTTP REQUEST PIPELINE
// =====================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FormBuilder API V1");
        c.RoutePrefix = string.Empty; // Swagger at root URL
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigins");
app.UseResponseCaching();

// Uncomment when authentication is added
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// =====================================
// HELPER METHODS
// =====================================
async Task SeedInitialData(
    ApplicationDbContext dbContext,
    MongoDbContext mongoContext,
    ILogger logger)
{
    // Check if data already exists
    if (await dbContext.Users.AnyAsync())
    {
        logger.LogInformation("Database already has data. Skipping seed.");
        return;
    }

    logger.LogInformation("Seeding initial data...");

    // Create admin user
    var adminUser = new User
    {
        UserId = Guid.NewGuid(),
        Email = "admin@formbuilder.com",
        Username = "admin",
        PasswordHash = "admin_hash_here", // Use proper hashing in production
        Role = UserRole.Admin,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    dbContext.Users.Add(adminUser);
    await dbContext.SaveChangesAsync();

    // Create sample form
    var sampleForm = new Form
    {
        Title = "Sample Welcome Form",
        Description = "Initial form for testing",
        CreatedBy = adminUser.UserId,
        IsPublished = true,
        Questions = new List<Question>
        {
            new Question
            {
                Label = "Your Name",
                Type = QuestionType.ShortText,
                Required = true,
                Order = 1
            }
        },
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    await mongoContext.Forms.InsertOneAsync(sampleForm);

    logger.LogInformation("✅ Initial data seeded successfully!");
}

// Make Program class public for testing
[ExcludeFromCodeCoverage]
public partial class Program
{
}