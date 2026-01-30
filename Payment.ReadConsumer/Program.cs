using Nest;
using Payment.ReadConsumer.Consumers;
using Payment.ReadConsumer.Infrastructure.Elastic;

var builder = Host.CreateApplicationBuilder(args);

var loggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var logger = loggerFactory.CreateLogger("Startup");

logger.LogInformation("Builder aşamasındayım");

// Elasticsearch client
builder.Services.AddSingleton<IElasticClient>(sp =>
{
   var uri = builder.Configuration.GetValue<string>("Elasticsearch:Uri");
    logger.LogInformation("Elasticsearch URI: {Uri}", uri);
    
    if (string.IsNullOrEmpty(uri))
        throw new Exception("Elasticsearch:Uri boş");

    var settings = new ConnectionSettings(new Uri(uri))
        .DefaultIndex("transactions");;

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
