using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Payment.WriteApi.Application.Commands;
using Payment.WriteApi.Infrastructure.Persistence;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddDbContext<WriteDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("MySql"))
);

builder.Services.AddScoped<CreateTransactionHandler>();

builder.Services.AddRateLimiter(opt =>
{
    opt.AddFixedWindowLimiter("write-limit", options =>
    {
        options.Window = TimeSpan.FromSeconds(1);
        options.PermitLimit = 10;
    });
});

//builder.Services.AddHealthChecks()
//    .AddDbContextCheck<WriteDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Prometheus metrics endpoint
app.UseMetricServer();
app.UseHttpMetrics();

app.UseRateLimiter();
app.MapHealthChecks("/health");

app.MapControllers();
app.Run();