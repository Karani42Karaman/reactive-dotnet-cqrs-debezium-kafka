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

                // Debezium envelope yapısını kontrol et
                if (!root.TryGetProperty("payload", out var payload))
                {
                    _logger.LogWarning("Message doesn't have payload property");
                    return;
                }

                // Operation type'ı kontrol et (c=create, u=update, d=delete, r=read/snapshot)
                var op = payload.TryGetProperty("op", out var opElement)
                    ? opElement.GetString()
                    : "r";

                _logger.LogInformation("Operation type: {Op}", op);

                // DELETE operasyonu - after null olur
                if (op == "d")
                {
                    if (payload.TryGetProperty("before", out var before) &&
                        before.ValueKind != JsonValueKind.Null)
                    {
                        var id = before.GetProperty("Id").GetInt64();
                        _logger.LogInformation("Deleting transaction ID: {Id}", id);

                        await _elastic.DeleteAsync<TransactionReadModel>(id, d =>
                            d.Index("transactions")
                        );
                    }
                    return;
                }

                // CREATE, UPDATE veya SNAPSHOT için after'ı kullan
                if (!payload.TryGetProperty("after", out var after))
                {
                    _logger.LogWarning("Payload doesn't have 'after' property");
                    return;
                }

                if (after.ValueKind == JsonValueKind.Null)
                {
                    _logger.LogInformation("'after' is null, skipping");
                    return;
                }

                // After nesnesinden model oluştur
                var model = new TransactionReadModel
                {
                    Id = after.GetProperty("Id").GetInt64(),
                    UserId = after.GetProperty("UserId").GetInt64(),
                    Amount = ParseDecimal(after.GetProperty("Amount")),
                    Currency = after.GetProperty("Currency").GetString() ?? "",
                    Status = after.GetProperty("Status").GetString() ?? ""
                };

                _logger.LogInformation(
                    "Processing transaction - ID: {Id}, UserId: {UserId}, Amount: {Amount}, Currency: {Currency}, Status: {Status}",
                    model.Id, model.UserId, model.Amount, model.Currency, model.Status
                );

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

        private decimal ParseDecimal(JsonElement element)
        {
            // Debezium decimal'ları double olarak gönderir (decimal.handling.mode: double)
            if (element.ValueKind == JsonValueKind.Number)
            {
                return (decimal)element.GetDouble();
            }

            // String olarak gelirse
            if (element.ValueKind == JsonValueKind.String)
            {
                if (decimal.TryParse(element.GetString(), out var result))
                {
                    return result;
                }
            }

            _logger.LogWarning("Could not parse decimal from: {Value}", element.GetRawText());
            return 0;
        }
    }
}