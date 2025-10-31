using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Backend.Models.Nosql;

public class Form
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("_id")]
    public string Id { get; set; }

    [BsonElement("title")] [BsonRequired] public string Title { get; set; }

    [BsonElement("description")] public string? Description { get; set; }

    [BsonElement("headerTitle")] public string? HeaderTitle { get; set; }

    [BsonElement("headerDescription")] public string? HeaderDescription { get; set; }

    [BsonElement("isPublished")] public bool IsPublished { get; set; } = false;

    [BsonElement("isDeleted")] public bool IsDeleted { get; set; } = false;
    
    [BsonElement("visibility")] public bool Visibility { get; set; } = true;

    [BsonElement("createdBy")]
    [BsonRequired]
    [BsonRepresentation(BsonType.String)]
    public Guid CreatedBy { get; set; }

    [BsonElement("publishedBy")]
    [BsonRepresentation(BsonType.String)]
    public Guid? PublishedBy { get; set; }

    [BsonElement("questions")] public List<Question> Questions { get; set; } = new List<Question>();

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}