using WebUi.Server.Models.RssPuller;
using System.Net.Sockets;
using System.Xml.Linq;

namespace WebUi.Server.Services;

public class NewLinksCollector : IHostedService, IDisposable
{
    const int ExecutionIntervalSeconds = 60;
    private readonly ILogger<NewLinksCollector> logger;
    private readonly IMongoRepository repository;
    private Timer timer = null!;

    private HttpClient httpClient = new HttpClient();

    public NewLinksCollector(ILogger<NewLinksCollector> logger, IMongoRepository repository)
    {
        this.logger = logger;
        this.repository = repository;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation($"{nameof(NewLinksCollector)} started");
        timer = new Timer(PullLatestRssLinks, null, TimeSpan.Zero,  TimeSpan.FromSeconds(ExecutionIntervalSeconds));
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
    }

    private async void PullLatestRssLinks(object? state)
    {
        logger.LogInformation($"Pulling Rss Links");
        var rssFeeds = await  repository.GetRssToPullAsync(DateTime.UtcNow);
        logger.LogDebug($"Feeds to pull: {string.Join(',', rssFeeds.Select(f=> f.Name))}");

        foreach (var rss in rssFeeds)
        {
            try
            {
                logger.LogDebug($"Pulling {rss.Name}");
                var response = await httpClient.GetAsync(rss.Url);
                var stringRssContent = await response.Content.ReadAsStringAsync();
                await repository.AddRssPullHistoryAsync(new RssPullHistory
                {
                    RssId = rss.Id,
                    UpdateTime = DateTime.UtcNow,
                    RssContent = stringRssContent
                });

                rss.LastPulled = DateTime.UtcNow;
                await repository.UpdatePullTimeAsync(rss);
                var articles = ParseRssArticles(stringRssContent, rss.Id);
                await repository.AddRssArticlesAsync(articles);
            }
            catch(Exception ex) when (
                ex is SocketException || 
                ex is HttpRequestException ||
                ex is TimeoutException)
            {
                rss.IsAlive = false;
                await repository.UpdateRssAliveStatusAsync(rss);
                logger.LogError(ex, $"Error while pulling {rss.Name}, {rss.Url}");
            }
        }
    }

    private List<RssArticle> ParseRssArticles(string content, string rssId)
    {
        XNamespace contentNs = @"http://purl.org/rss/1.0/modules/content/";
        XNamespace atomNs = @"http://www.w3.org/2005/Atom";

        var xml = content.Replace("\n", "").Replace("\r", "");
        try
        {
            var xdoc = XDocument.Parse(xml);

            if(xdoc?.Root?.Name.LocalName == "rss" || xdoc?.Root?.Name.LocalName == "channel")
            {
                var elements = xdoc.Root.Element("channel").Elements("item").Select(
                    x => 
                    new  RssArticle{ 
                        RssId = rssId,
                        Title = x.Element("title").Value,
                        Url = x.Element("link").Value,
                        PublishDate = x.Element("pubDate").Value,
                        Description = x.Element("description").Value,
                        RssContent = x?.Element(contentNs + "encoded")?.Value,
                        IsInvalidContent = false
                    }).ToList();
                //ToDo: Add error handling
                return elements;
            }
            else if(xdoc?.Root?.Name.LocalName == "feed")
            {
                var elements = xdoc?.Root?.Elements(atomNs+"entry")?.Select(
                x => 
                new  RssArticle{ 
                    RssId = rssId,
                    Title = x.Element(atomNs+"title")?.Value,
                    Url = x.Element(atomNs+"link")?.Attribute("href")?.Value,
                    PublishDate = x.Element(atomNs+"published")?.Value,
                    Description = x.Element(atomNs+"summary")?.Value,
                    RssContent = x?.Element(atomNs+"encoded")?.Value,
                    IsInvalidContent = false
                }).ToList();
                return elements;
            }
            return new List<RssArticle>();
        }
        catch(Exception ex)
        {
            logger.LogError(ex, $"Error while parsing {rssId}");
            logger.LogDebug(xml);
            throw;
        }
    }
}