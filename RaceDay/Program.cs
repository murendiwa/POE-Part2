using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Services;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

builder.Services.AddSingleton(_ => new BlobServiceClient(
    builder.Configuration["DefaultEndpointsProtocol=https;AccountName=raceday779;AccountKey=YiPCLZdYITl6bqTltzxlShs+1YUwRAoNg6piLNjxJa8tetrwHC3wXo2S1jSfp6ZFAqAABJ8bhS5w+AStQ0Z7Ww==;EndpointSuffix=core.windows.net"]
        ?? throw new InvalidOperationException("Azure:ConnectionString is not configured.")));
builder.Services.AddScoped<BlobStorageService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseSession();
app.UseAuthorization();
app.MapControllers();

app.Run();