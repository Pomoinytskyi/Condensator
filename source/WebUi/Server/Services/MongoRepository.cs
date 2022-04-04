using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WebUi.Server.Models;
using WebUi.Server.Models.RssPuller;

namespace WebUi.Server.Services;

public interface IMongoRepository
{
    Task<List<RssRecord>> GetRssAsync();
    Task AddRssAsync(IEnumerable<RssRecord> rssRecords);
    Task<List<RssRecord>> GetRssToPullAsync(DateTime pullTime);
    Task AddRssPullHistoryAsync(RssPullHistory rssPullHistory);
    Task UpdatePullTimeAsync(RssRecord rssRecord);
    Task UpdateRssAliveStatusAsync(RssRecord rssRecord);
    Task AddRssArticlesAsync(List<RssArticle> articles);
}

public class MongoRepository : IMongoRepository
{
    private const string RssCollectionName = "RssFeeds";
    private const string RssPullHistoryCollectionName = "RssPullHistory";
    private const string RssArticlesCollectionName = "RssArticles";
    private readonly IMongoCollection<RssRecord> rssCollection;
    private readonly IMongoCollection<RssPullHistory> rssPullHistoryCollection;
    private readonly IMongoCollection<RssArticle> rssArticlesCollection;

    public MongoRepository(IOptions<MongoDatabaseConfiguration> mongoDatabaseSettings)
    {
        var mongoClient = new MongoClient(mongoDatabaseSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(mongoDatabaseSettings.Value.DatabaseName);
        rssCollection = mongoDatabase.GetCollection<RssRecord>(RssCollectionName);
        rssPullHistoryCollection = mongoDatabase.GetCollection<RssPullHistory>(RssPullHistoryCollectionName);
        rssArticlesCollection = mongoDatabase.GetCollection<RssArticle>(RssArticlesCollectionName);
    }

    public async Task<List<RssRecord>> GetRssAsync() => await rssCollection.Find(_ => true).ToListAsync();
    public async Task AddRssAsync(IEnumerable<RssRecord> rssRecords) => await rssCollection.InsertManyAsync(rssRecords);

    public async Task<List<RssRecord>> GetRssToPullAsync(DateTime pullTime)
    {
        var resultCursor = await rssCollection.FindAsync(rss => rss.IsAlive && rss.LastPulled < pullTime - TimeSpan.FromDays(1));
        var searchResult = await resultCursor.ToListAsync();
        var result = searchResult.Where(r=> r.LastPulled + r.PullInterval < pullTime);
        return result.ToList();
    }

    public async Task AddRssPullHistoryAsync(RssPullHistory rssPullHistory) => await rssPullHistoryCollection.InsertOneAsync(rssPullHistory);

    public async Task UpdatePullTimeAsync(RssRecord rssRecord)
    {
        var filter = Builders<RssRecord>.Filter.Eq(r => r.Id, rssRecord.Id);
        var update = Builders<RssRecord>.Update.Set(r => r.LastPulled, rssRecord.LastPulled);
        await rssCollection.UpdateOneAsync(filter, update);
    }

    public async Task UpdateRssAliveStatusAsync(RssRecord rssRecord)
    {
        var filter = Builders<RssRecord>.Filter.Eq(r => r.Id, rssRecord.Id);
        var update = Builders<RssRecord>.Update.Set(r => r.IsAlive, rssRecord.IsAlive);
        await rssCollection.UpdateOneAsync(filter, update);
    }

    public async Task AddRssArticlesAsync(List<RssArticle> articles) => await rssArticlesCollection.InsertManyAsync(articles);
}