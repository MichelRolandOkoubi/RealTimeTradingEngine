using Confluent.Kafka;
using StackExchange.Redis;
using TradingEngine.Api.Hubs;
using TradingEngine.Api.Modules;
using TradingEngine.Api.Services;
using TradingEngine.MatchingEngine.Services; // Need to share the RedisOrderBook implementation
using TradingEngine.Shared.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Redis
var redisConn = ConnectionMultiplexer.Connect("localhost:6379");
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConn);
builder.Services.AddScoped<IRedisOrderBook, RedisOrderBook>();

// Kafka Producer
var producerConfig = new ProducerConfig { BootstrapServers = "localhost:19092" };
builder.Services.AddSingleton<IProducer<string, string>>(
    new ProducerBuilder<string, string>(producerConfig).Build());

// Background consumers
builder.Services.AddHostedService<TradeConsumer>();
builder.Services.AddHostedService<OrderBookPollingService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.MapHub<TradingHub>("/tradingHub");
app.MapOrderEndpoints();

app.Run();
