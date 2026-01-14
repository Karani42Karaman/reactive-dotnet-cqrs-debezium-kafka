using Nest;
using Payment.ReadConsumer.Consumers;
using Payment.ReadConsumer.Infrastructure.Elastic;

var builder = Host.CreateApplicationBuilder(args);

// Elasticsearch client
builder.Services.AddSingleton<IElasticClient>(sp =>
{
    var settings = new ConnectionSettings(new Uri("http://elasticsearch:9200"))
        .DefaultIndex("transactions");

    return new ElasticClient(settings);
});

//Kafka Worker
builder.Services.AddHostedService<TransactionConsumer>();

var app = builder.Build();

//  Index ensure (startup'ta)
using (var scope = app.Services.CreateScope())
{
    var elastic = scope.ServiceProvider.GetRequiredService<IElasticClient>();
    await ElasticIndexInitializer.EnsureIndexAsync(elastic);
}

app.Run();
