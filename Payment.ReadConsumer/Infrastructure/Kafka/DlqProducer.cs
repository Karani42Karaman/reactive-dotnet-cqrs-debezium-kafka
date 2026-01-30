using Confluent.Kafka;


namespace Payment.ReadConsumer.Infrastructure.Kafka
{
    public class DlqProducer
    {
        private readonly IProducer<string, string> _producer;

    public DlqProducer(IConfiguration configuration)  
    {
        var bootstrapServers = configuration["Kafka:BootstrapServers"];

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

        public async Task PublishAsync(string topic, string key, string value)
        {
            await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = key,
                Value = value
            });
        }
    }

}
