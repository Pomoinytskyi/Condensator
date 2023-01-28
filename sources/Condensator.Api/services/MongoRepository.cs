using Condensator.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Condensator.Api.services
{
	public interface IRepository
	{
		Task<List<NewsFeed>> GetNewsFeedsAsync();
		Task AddNewsFeedsAsync(IEnumerable<NewsFeed> newsFeed);
		Task UpdateNewsFeedAsync(string id, NewsFeed newsFeed);
		Task DeleteNewsFeedAsync(string id);
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
	}
}
