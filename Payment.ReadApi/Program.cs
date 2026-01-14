using Nest;
using Payment.ReadApi.Infrastructure.Elastic;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Elasticsearch client
builder.Services.AddSingleton<IElasticClient>(sp =>
{
    var settings = new ConnectionSettings(new Uri("http://elasticsearch:9200"))
        .DefaultIndex("transactions");

    return new ElasticClient(settings);
});

// Read repository
builder.Services.AddScoped<TransactionReadRepository>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHealthChecks();

var app = builder.Build();

// Prometheus metrics endpoint
app.UseMetricServer();
app.UseHttpMetrics();

app.MapHealthChecks("/health");
app.MapControllers();
app.Run();