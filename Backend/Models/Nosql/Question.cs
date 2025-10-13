using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Backend.Models.Nosql;

public class Question
{
    [BsonElement("questionId")]
    [BsonRequired]
    public string QuestionId { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("label")] 
    [BsonRequired] 
    public string Label { get; set; }

    [BsonElement("description")] 
    public string? Description { get; set; }

    [BsonElement("type")]
    [BsonRequired]
    public string Type { get; set; } // shortText, longText, number, date, singleSelect, multiSelect, file

    [BsonElement("required")] 
    public bool Required { get; set; } = false;

    [BsonElement("options")]
    [BsonIgnoreIfNull]
    public List<Option>? Options { get; set; } // CHANGED: For single/multi select

    [BsonElement("order")] 
    public int Order { get; set; }
}

public class Option
{
    [BsonElement("optionId")]
    public string OptionId { get; set; } = Guid.NewGuid().ToString();
    
    [BsonElement("label")]
    [BsonRequired]
    public string Label { get; set; } = string.Empty;
}