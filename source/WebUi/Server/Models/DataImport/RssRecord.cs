using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebUi.Server.Models.DataImport;

public class RssRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Name { get; set; } = null!;

    public string  Url { get; set; } = null!;

    public string Domain { get; set; } = null!;

    public string UserId { get; set; } = null!;
}