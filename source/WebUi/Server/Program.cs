using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using WebUi.Server.Data;
using WebUi.Server.Models;
using WebUi.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var cosmosConfig = builder.Services.Configure<MongoDatabaseConfiguration>(builder.Configuration.GetSection("MongoDatabase"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddSingleton<IMongoRepository, MongoRepository>();
builder.Services.AddHostedService<NewLinksCollector>();

builder.Services.AddIdentityServer().AddApiAuthorization<ApplicationUser, ApplicationDbContext>();
builder.Services.AddAuthentication().AddIdentityServerJwt();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// var  MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy(name: MyAllowSpecificOrigins,
//                       builder =>
//                       {
//                           builder.WithOrigins("https://localhost:7256");
//                       });
// });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();



app.UseRouting();
// app.UseCors(MyAllowSpecificOrigins);

app.MapControllerRoute(name: "routes", pattern: "{controller:routes}");

app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();


app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
