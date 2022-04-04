using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebUi.Server.Models.RssPuller;

public class RssArticle
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string RssId { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public string? RssContent { get; set; }
    public string? Content { get; set; }
    public bool IsInvalidContent { get; set; } = false;
    public string PublishDate { get; set; }
    public string Description { get; set; }

}