using StackExchange.Redis;
using Tanabata.Api;
using Tanabata.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var redis = ConnectionMultiplexer.Connect("localhost");
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ILiveLocationStore, LiveLocationStore>();
builder.Services.AddSingleton<IOsmService, OsmService>();


// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<SkyStreamService>();

app.Run();