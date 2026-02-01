using StackExchange.Redis;
using Tanabata.Api;
using Tanabata.Api.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSignalR();
builder.Services.AddCors(options => options
    .AddPolicy("AllowAll", p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

var redis = ConnectionMultiplexer.Connect("localhost");
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

builder.Services.AddHttpClient();

builder.Services.AddSingleton<ILiveLocationStore, LiveLocationStore>();
builder.Services.AddSingleton<IOsmService, OsmService>();

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<SkyStreamService>();

app.UseCors("AllowAll");
app.MapHub<SignalingHub>("/signaling");

app.Run();