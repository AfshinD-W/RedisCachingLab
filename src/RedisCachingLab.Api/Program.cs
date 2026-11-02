using RedisCachingLab.Core.Entities;
using RedisCachingLab.Infrastructure.Redis;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton(new RedisConnection("localhost:6380"));
builder.Services.AddSingleton<RedisCacheService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/redis-test", async (RedisConnection redis) =>
{
    IDatabase database = redis.Connection.GetDatabase();

    await database.StringSetAsync("app:message", "Hello Redis!", TimeSpan.FromSeconds(30));

    RedisValue value = await database.StringGetAsync("app:message");

    TimeSpan? ttl = await database.KeyTimeToLiveAsync("app:message");

    return Results.Ok(new
    {
        Key = "app:message",
        Value = value.ToString(),
        TTL = ttl?.TotalSeconds
    });
});

app.MapGet("/cache/products", async (RedisCacheService cache) =>
{
    var product = new Product
    {
        Id = 100,
        Name = "Gaming Laptop",
        Price = 1500,
        Stock = 20
    };

    await cache.SetAsync($"product:{product.Id}", product, TimeSpan.FromMinutes(5));

    return Results.Ok(product);
});

app.MapGet("/cache/products/{id:int}", async (int id, RedisCacheService cache) =>
{
    var product = await cache.GetTAsync<Product>($"product:{id}");

    if (product is null)
        return Results.NotFound();

    return Results.Ok(product);
});

app.MapGet("/cache/hash/products", async (RedisCacheService cache) =>
{
    var product = new Product
    {
        Id = 200,
        Name = "Mechanical Keyboard",
        Price = 100,
        Stock = 15
    };

    await cache.SetHashAsync($"product:{product.Id}", product);

    return Results.Ok(product);
});

app.MapGet("/cache/hash/products/{id:int}", async (
    int id,
    RedisCacheService cache) =>
{
    Product? product = await cache.GetHashAsync<Product>($"product:{id}");

    return product is null
        ? Results.NotFound()
        : Results.Ok(product);
});

app.Run();
