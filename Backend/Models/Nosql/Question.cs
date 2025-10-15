using System;
using System.Collections.Generic;
using Backend.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Backend.Models.Nosql;

public class Question
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("_id")]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("label")] 
    [BsonRequired] 
    public string Label { get; set; }

    [BsonElement("description")] 
    public string? Description { get; set; }

    [BsonElement("type")]
    [BsonRequired]
    [BsonRepresentation(BsonType.String)]
    public QuestionType Type { get; set; } 
    
    
    [BsonElement("required")] 
    public bool Required { get; set; } = false;
    
    [BsonElement("dateFormat")] 
    [BsonIgnoreIfNull]
    public string DateFormat { get; set; } = string.Empty;

    [BsonElement("options")]
    [BsonIgnoreIfNull]
    public List<Option>? Options { get; set; } // CHANGED: For single/multi select

    [BsonElement("order")] 
    public int Order { get; set; }
}

public class Option
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("_id")]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    
    [BsonElement("label")]
    [BsonRequired]
    public string Label { get; set; } = string.Empty;
}