using AutoMapper;
using Condensator.Api.Models;
using Condensator.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Condensator.Api.Extensions;
using Public = Condensator.Common.Entities;
    
var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(CreateMapper());
builder.Services.AddHostedService<NewLinksCollector>();


builder.Services.Configure<MongoDatabaseConfiguration>(builder.Configuration.GetSection("MongoDatabase"));
builder.Services.AddSingleton<IRepository, MongoRepository>();

builder.Services.InitializeRabitMq();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalhostCors",
        policy =>
        {
            policy.WithOrigins("https://localhost:7295", "http://localhost:5203")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});


var app = builder.Build();
app.UseCors("LocalhostCors");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/newsFeeds", async ([FromServices] IRepository repository, [FromServices] IMapper mapper) => 
{
    var data = await repository.GetNewsFeedsAsync();
    var mapped = data.Select(d => mapper.Map<Public.NewsFeed>(d));
    return mapped;
})
.WithName("Get all news feed")
.WithOpenApi();

app.MapPost("/newsFeeds", async (Public.NewsFeed newUiFeed, [FromServices] IRepository repository, [FromServices] IMapper mapper) =>
{
    NewsFeed newFeed = mapper.Map<NewsFeed>(newUiFeed);
    await repository.AddNewsFeedsAsync(new List<NewsFeed> { newFeed });
})
.WithName("Add new news feed")
.WithOpenApi();

app.MapPut("/newsFeeds", async ([FromBody]Public.NewsFeed newValue, [FromServices] IRepository repository, [FromServices] IMapper mapper) =>
{
	NewsFeed newFeed = mapper.Map<NewsFeed>(newValue);
	await repository.UpdateNewsFeedAsync(newValue.Id, newFeed);
})
.WithName("Update news feed")
.WithOpenApi();

app.MapDelete("/newsFeeds/{id}", async (string id, [FromServices] IRepository repository, [FromServices] IMapper mapper) =>
{
	await repository.DeleteNewsFeedAsync(id);
})
.WithName("Delete news feed")
.WithOpenApi();

app.MapGet("/feed/{feedId}", async (string feedId, [FromServices] IRepository repository, [FromServices] IMapper mapper) =>
{
	var data = await repository.GetFeedSummaries(feedId);
	var mapped = data.Select(d => mapper.Map<Public.Article>(d));
	return mapped;
})
.WithName("Get feed articles")
.WithOpenApi();

app.Run();

static IMapper CreateMapper()
{
    var config = new MapperConfiguration(cfg =>
    {
        cfg.CreateMap<NewsFeed, Public.NewsFeed>().ReverseMap();
		cfg.CreateMap<Article, Public.Article>()
			.ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.FinalUrl))
			.ReverseMap();
	});

    IMapper mapper = config.CreateMapper();
    return mapper;
}

