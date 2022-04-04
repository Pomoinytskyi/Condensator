using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebUi.Server.Models.RssPuller;

public class RssRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string  Url { get; set; } = null!;

    public string Domain { get; set; } = null!;

    public DateTime LastPulled { get; set; }
    public TimeSpan PullInterval { get; set; }
    public bool IsAlive { get; set; }
}