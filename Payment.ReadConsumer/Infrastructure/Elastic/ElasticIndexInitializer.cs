using Nest;


namespace Payment.ReadConsumer.Infrastructure.Elastic
{
    public static class ElasticIndexInitializer
    {
        public static async Task EnsureIndexAsync(IElasticClient client)
        {
            var exists = await client.Indices.ExistsAsync("transactions");

            if (!exists.Exists)
            {
                await client.Indices.CreateAsync("transactions", c =>
                    c.Map(m => m.AutoMap())
                );
            }
        }
    }
}
