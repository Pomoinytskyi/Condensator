using Condensator.Api.Models.Messages;
using RabbitMQ.Client;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Condensator.Api.Services;

public class NewLinksCollector : IHostedService, IDisposable
{
	const int ExecutionIntervalSeconds = 60;
	private TimeSpan PullInterval => TimeSpan.FromSeconds(ExecutionIntervalSeconds);
	
	private readonly ILogger<NewLinksCollector> logger;
	private readonly IRepository repository;
	private readonly ConnectionFactory rabbitConnectionFactory;
	private Timer timer = null!;
	private IConnection rabbitMqConnection;
	IModel rabbitMqChannel;


	private readonly HttpClient httpClient = new HttpClient();

	public NewLinksCollector(ILogger<NewLinksCollector> logger, IRepository repository, ConnectionFactory rabbitConnectionFactory)
	{
		this.logger = logger;
		this.repository = repository;

		rabbitMqConnection = rabbitConnectionFactory.CreateConnection();
		rabbitMqChannel = rabbitMqConnection.CreateModel();		
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		logger.LogInformation($"{nameof(NewLinksCollector)} started");
		timer = new Timer(PullLatestRssLinks, null, TimeSpan.Zero, TimeSpan.FromSeconds(ExecutionIntervalSeconds));
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		logger.LogInformation($"{nameof(NewLinksCollector)} stopped");
		timer?.Change(Timeout.Infinite, 0);
		return Task.CompletedTask;
	}

	public void Dispose()
	{
		timer?.Dispose();

		rabbitMqChannel.Close();
		rabbitMqChannel.Dispose();

		rabbitMqConnection.Close();
		rabbitMqConnection.Dispose();
	}

	private async void PullLatestRssLinks(object? state)
	{
		logger.LogInformation($"Pulling Rss Links");
		var feeds = await repository.GetFeedsToPull(DateTime.UtcNow - PullInterval);
		logger.LogDebug($"Feeds to pull: {string.Join(',', feeds.Select(f => f.Name))}");

		foreach (var feed in feeds)
		{
			try
			{
				logger.LogDebug($"Pulling {feed.Name}");
				var response = await httpClient.GetAsync(feed.Url);
				var stringRssContent = await response.Content.ReadAsStringAsync();
				
				feed.LastPulled = DateTime.UtcNow;
				await repository.UpdatePullTimeAsync(feed);
				
				var articles = ParseRssArticles(stringRssContent, feed.Id);
				SendDownloadRequests(articles);
				//await repository.AddRssArticlesAsync(articles);
			}
			catch (Exception ex) when (
				ex is SocketException ||
				ex is HttpRequestException ||
				ex is TimeoutException)
			{
				//feed.IsAlive = false;
				//await repository.UpdateRssAliveStatusAsync(feed);
				logger.LogError(ex, $"Error while pulling {feed.Name}, {feed.Url}");
			}
		}
	}

	private List<DownloadRequest> ParseRssArticles(string content, string rssId)
	{
		XNamespace contentNs = @"http://purl.org/rss/1.0/modules/content/";
		XNamespace atomNs = @"http://www.w3.org/2005/Atom";

		var xml = content.Replace("\n", "").Replace("\r", "");
		try
		{
			var xdoc = XDocument.Parse(xml);

			if (xdoc?.Root?.Name.LocalName == "rss" || xdoc?.Root?.Name.LocalName == "channel")
			{
				var elements = xdoc.Root.Element("channel").Elements("item").Select(
					x =>
					new DownloadRequest
					{
						Name = rssId,
						Url = x.Element("link").Value,
						TimeStamp = DateTime.UtcNow.ToString()
						//RssId = rssId,
						//Title = x.Element("title").Value,
						//Url = x.Element("link").Value,
						//PublishDate = x.Element("pubDate").Value,
						//Description = x.Element("description").Value,
						//RssContent = x?.Element(contentNs + "encoded")?.Value,
						//IsInvalidContent = false
					}).ToList();
				//ToDo: Add error handling
				return elements;
			}
			else if (xdoc?.Root?.Name.LocalName == "feed")
			{
				var downloadRequests = xdoc?.Root?.Elements(atomNs + "entry")?.Select(
				x =>
				new DownloadRequest
				{
					Name = rssId,
					Url = x.Element(atomNs + "link")?.Attribute("href")?.Value,
					TimeStamp = DateTime.UtcNow.ToString()
					//RssId = rssId,
					//Title = x.Element(atomNs + "title")?.Value,
					//Url = x.Element(atomNs + "link")?.Attribute("href")?.Value,
					//PublishDate = x.Element(atomNs + "published")?.Value,
					//Description = x.Element(atomNs + "summary")?.Value,
					//RssContent = x?.Element(atomNs + "encoded")?.Value,
					//IsInvalidContent = false
				}).ToList();
				return downloadRequests;
			}
			return new List<DownloadRequest>();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, $"Error while parsing {rssId}");
			logger.LogDebug(xml);
			throw;
		}
	}

	private void SendDownloadRequests(List<DownloadRequest>requestObjects)
	{
		foreach (var obj in requestObjects) 
		{
			var serialized = JsonSerializer.Serialize(obj);
			var bytes = Encoding.UTF8.GetBytes(serialized);
			rabbitMqChannel.BasicPublish(exchange: "", routingKey: "Download", basicProperties: null, body: bytes);
		}		
	}
}