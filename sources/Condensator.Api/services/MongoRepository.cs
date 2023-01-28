using Condensator.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Condensator.Api.Services
{
	public interface IRepository
	{
		Task<List<NewsFeed>> GetNewsFeedsAsync();
		Task AddNewsFeedsAsync(IEnumerable<NewsFeed> newsFeed);
		Task UpdateNewsFeedAsync(string id, NewsFeed newsFeed);
		Task DeleteNewsFeedAsync(string id);
		Task<List<NewsFeed>> GetFeedsToPull(DateTime pulledBeforeTime);
		Task UpdatePullTimeAsync(NewsFeed updatedFeed);
	}

	public class MongoRepository : IRepository
	{
		private const string NewsFeedCollectionName = "NewsFeeds";

		private readonly IMongoCollection<NewsFeed> newsFeedsCollection;

		public MongoRepository(IOptions<MongoDatabaseConfiguration> mongoDatabaseSettings)
		{
			var mongoClient = new MongoClient(mongoDatabaseSettings.Value.ConnectionString);
			var mongoDatabase = mongoClient.GetDatabase(mongoDatabaseSettings.Value.DatabaseName);
			newsFeedsCollection = mongoDatabase.GetCollection<NewsFeed>(NewsFeedCollectionName);
		}

		public async Task<List<NewsFeed>> GetNewsFeedsAsync() => await newsFeedsCollection.Find(_ => true).ToListAsync();
		public async Task AddNewsFeedsAsync(IEnumerable<NewsFeed> newsFeeds)
		{
			foreach (var nf in newsFeeds)
			{
				if (nf.Id == null)
				{
					nf.Id = Guid.NewGuid().ToString();
				}
			}
			
			await newsFeedsCollection.InsertManyAsync(newsFeeds);
		}

		public async Task UpdateNewsFeedAsync(string id, NewsFeed newValue)
		{
			await newsFeedsCollection.ReplaceOneAsync<NewsFeed>(f => f.Id == id, newValue);
		}

		public async Task DeleteNewsFeedAsync(string id)
		{
			await newsFeedsCollection.DeleteOneAsync<NewsFeed>(f => f.Id == id);
		}

		public async Task<List<NewsFeed>> GetFeedsToPull(DateTime pulledBeforeTime)
		{
			var queryResult = await newsFeedsCollection.FindAsync(f => f.LastPulled < pulledBeforeTime);
			return queryResult.ToList();
		}

		public async Task UpdatePullTimeAsync(NewsFeed updatedFeed)
		{
			var filter = Builders<NewsFeed>.Filter.Eq(r => r.Id, updatedFeed.Id);
			var update = Builders<NewsFeed>.Update.Set(r => r.LastPulled, updatedFeed.LastPulled);
			await newsFeedsCollection.UpdateOneAsync(filter, update);
		}
	}
}
