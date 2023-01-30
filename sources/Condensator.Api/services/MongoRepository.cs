using Condensator.Api.Models;
using Condensator.Api.Models.Messages;

using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
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
		Task<List<Article>> GetFeedSummaries(string feedId, int limit = 100);
		Task<List<DownloadRequest>>FilterNewArticles(List<DownloadRequest> articles);
	}

	public class MongoRepository : IRepository
	{
		private const string NewsFeedCollectionName = "NewsFeeds";
		private const string ArticleCollectionName = "Articles";

		private readonly IMongoCollection<NewsFeed> newsFeedsCollection;
		private readonly IMongoCollection<Article> articlesCollection;

		public MongoRepository(IOptions<MongoDatabaseConfiguration> mongoDatabaseSettings)
		{
			BsonClassMap.RegisterClassMap<Article>(cm =>
			{
				cm.AutoMap();
				cm.MapIdProperty(c => c.Id)
					.SetIdGenerator(StringObjectIdGenerator.Instance)
					.SetSerializer(new StringSerializer(BsonType.ObjectId));
			});

			var mongoClient = new MongoClient(mongoDatabaseSettings.Value.ConnectionString);
			var mongoDatabase = mongoClient.GetDatabase(mongoDatabaseSettings.Value.DatabaseName);
			newsFeedsCollection = mongoDatabase.GetCollection<NewsFeed>(NewsFeedCollectionName);
			articlesCollection = mongoDatabase.GetCollection<Article>(ArticleCollectionName);
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

		public async Task<List<Article>> GetFeedSummaries(string feedId, int limit = 100)
		{
			var queryResult = await articlesCollection.Find<Article>(a => a.FeedId == feedId && a.Summary != null)
				.Limit(limit)
				.ToListAsync();
			return queryResult;

		}

		public async Task<List<DownloadRequest>> FilterNewArticles(List<DownloadRequest> articles)
		{
			var result = new List<DownloadRequest>();
			foreach (var article in articles)
			{
				var queryResult = await articlesCollection.Find(_ => _.FinalUrl == article.Url).AnyAsync();
				if (!queryResult)
				{
					result.Add(article);
				}
			}
			return result;
		}
	}
}
