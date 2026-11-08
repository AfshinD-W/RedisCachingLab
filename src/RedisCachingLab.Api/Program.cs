using Microsoft.EntityFrameworkCore;
using RedisCachingLab.Core.Interfaces;
using RedisCachingLab.Infrastructure.BackgroundServices;
using RedisCachingLab.Infrastructure.Persistence;
using RedisCachingLab.Infrastructure.Persistence.Repositories;
using RedisCachingLab.Infrastructure.Redis;
using RedisCachingLab.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<OutboxProcessor>();

//Redis registeration
builder.Services.AddSingleton(new RedisConnection("127.0.0.1:6380"));
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

//Sql registeration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ));

//Services
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();


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
        options.DisplayRequestDuration();
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
