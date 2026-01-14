using Confluent.Kafka;
using Nest;
using Payment.ReadConsumer.Infrastructure.Kafka;
using Payment.ReadConsumer.Infrastructure.Retry;
using Payment.ReadConsumer.Models;
using System.Text.Json;

namespace Payment.ReadConsumer.Consumers
{
    public class TransactionConsumer : BackgroundService
    {
        private readonly IElasticClient _elastic;
        private readonly DlqProducer _dlq;
        private readonly ILogger<TransactionConsumer> _logger;

        public TransactionConsumer(
            IElasticClient elastic,
            ILogger<TransactionConsumer> logger)
        {
            _elastic = elastic;
            _dlq = new DlqProducer();
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:29092",
                GroupId = "payment-read-consumer",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                SessionTimeoutMs = 45000,
                MaxPollIntervalMs = 300000
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();

            // Debezium'un oluşturduğu topic adı
            var topic = "dbserver1.payment.payment.dbo.Transactions";
            consumer.Subscribe(topic);

            _logger.LogInformation("Consumer subscribed to topic: {Topic}", topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);

                    if (cr?.Message?.Value == null)
                        continue;

                    _logger.LogInformation("Received message: {Message}", cr.Message.Value);

                    try
                    {
                        await RetryPolicy.ExecuteAsync(async () =>
                        {
                            await ProcessMessage(cr.Message.Value);
                        });

                        consumer.Commit(cr);
                        _logger.LogInformation("Message processed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process message after retries");

                        // DLQ'ya gönder
                        await _dlq.PublishAsync(
                            "payment.transactions.dlq",
                            cr.Message.Key ?? "null",
                            cr.Message.Value
                        );

                        consumer.Commit(cr);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Consumer is shutting down");
                    break;
                }
            }

            consumer.Close();
        }

        private async Task ProcessMessage(string value)
        {
            try
            {
                var doc = JsonDocument.Parse(value);
                var root = doc.RootElement;

                //// Debezium message format'ı kontrol et
                //if (!root.TryGetProperty("payload", out var payload))
                //{
                //    _logger.LogWarning("Message doesn't have payload property");
                //    return;
                //}

                //// After değerini al (yeni state)
                //if (!payload.TryGetProperty("after", out var after))
                //{
                //    _logger.LogWarning("Payload doesn't have 'after' property (probably a DELETE)");
                //    return;
                //}

                //if (after.ValueKind == JsonValueKind.Null)
                //{
                //    _logger.LogInformation("'after' is null (DELETE operation), skipping");
                //    return;
                //}

                var model = JsonSerializer.Deserialize<TransactionReadModel>(root);

                if (model == null)
                {
                    _logger.LogWarning("Failed to deserialize model");
                    return;
                }

                _logger.LogInformation("Indexing transaction ID: {Id}", model.Id);

                var response = await _elastic.IndexAsync(model, i =>
                    i.Index("transactions")
                     .Id(model.Id)
                );

                if (!response.IsValid)
                {
                    _logger.LogError("Elasticsearch indexing failed: {Error}", response.DebugInformation);
                    throw new Exception($"Elasticsearch error: {response.OriginalException?.Message}");
                }

                _logger.LogInformation("Successfully indexed to Elasticsearch");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error");
                throw;
            }
        }
    }
}