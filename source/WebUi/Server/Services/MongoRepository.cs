using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WebUi.Server.Models;
using WebUi.Server.Models.DataImport;

namespace WebUi.Server.Servoces;

public interface IMongoRepository
{
    Task<List<RssRecord>> GetRssAsync();
    Task AddRssAsync(IEnumerable<RssRecord> rssRecords);
}

public class MongoRepository : IMongoRepository
{
    private const string RssCollectionName = "RssFeeds";
    private readonly IMongoCollection<RssRecord> rssCollection;

    public MongoRepository(IOptions<MongoDatabaseConfiguration> mongoDatabaseSettings)
    {
        var mongoClient = new MongoClient(mongoDatabaseSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(mongoDatabaseSettings.Value.DatabaseName);
        rssCollection = mongoDatabase.GetCollection<RssRecord>(RssCollectionName);
    }

    public async Task<List<RssRecord>> GetRssAsync() => await rssCollection.Find(_ => true).ToListAsync();
    public async Task AddRssAsync(IEnumerable<RssRecord> rssRecords)
    {
        await rssCollection.InsertManyAsync(rssRecords);
    }
}