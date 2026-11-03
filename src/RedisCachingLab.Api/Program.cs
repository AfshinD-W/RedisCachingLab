using Microsoft.EntityFrameworkCore;
using RedisCachingLab.Core.Entities;
using RedisCachingLab.Core.Interfaces;
using RedisCachingLab.Infrastructure.Persistence;
using RedisCachingLab.Infrastructure.Persistence.Repositories;
using RedisCachingLab.Infrastructure.Redis;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(new RedisConnection("localhost:6380"));
builder.Services.AddSingleton<RedisCacheService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ));

builder.Services.AddScoped<IProductRepository, ProductRepository>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RedisCachingLab API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
