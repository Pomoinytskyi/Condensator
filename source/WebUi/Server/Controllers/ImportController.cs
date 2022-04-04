using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUi.Server.Models.RssPuller;
using WebUi.Server.Services;
using WebUi.Shared;

namespace WebUi.Server.Controllers;

[Authorize]
[ApiController]
[Route("[controller]/[action]")]
public class ImportController : ControllerBase
{
    //ToDo: Move to config
    private const int DefaultPullIntervalSeconds = 13 * 60;

    private IMongoRepository repository;
    private ILogger<ImportController> logger;
    public ImportController(IMongoRepository repository
    , ILogger<ImportController> logger
    )
    {
        this.repository = repository;
        this.logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> UploadOmpFile([FromForm]IFormFile file)
    {
        var loggerScope = logger.BeginScope($"{nameof(ImportController)}.{nameof(UploadOmpFile)}");
        string filePath = string.Empty;
        try
        {
            var fileName = Guid.NewGuid().ToString() + ".xml";
            filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);
            logger.LogInformation($"Uploading file {fileName} to {filePath}");
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var rssRecords = LoadRssFromOpmlFile(filePath);
            logger.LogInformation("Parsed {rssRecordsCount} records from {rssFileName}", rssRecords.Count, fileName);
            await repository.AddRssAsync(rssRecords);
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while uploading file");
            return BadRequest(ex.Message);
        }
        finally
        {
            if(System.IO.File.Exists(filePath))
            {
                 System.IO.File.Delete(filePath);
            }

            loggerScope.Dispose();
        }
    }

    private List<RssRecord> LoadRssFromOpmlFile(string fileName)
    {
        XDocument opml = XDocument.Load(fileName);
        logger.LogInformation(opml.Root.ToString());
        var rssRecords = opml.Root?.Elements("outline")
            .Select(outline => new RssRecord
            {
                Name = outline.Attribute("title")!.Value,
                Url = outline.Attribute("xmlUrl")!.Value,
                Domain = outline.Attribute("htmlUrl")!.Value,
                LastPulled = DateTime.MinValue,
                PullInterval = TimeSpan.FromSeconds(DefaultPullIntervalSeconds),
                IsAlive = true
            }).ToList();
        return rssRecords ?? new List<RssRecord>();
    }

    [HttpGet]
    public IEnumerable<WeatherForecast> GetData()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55)
        })
        .ToArray();
    }

}