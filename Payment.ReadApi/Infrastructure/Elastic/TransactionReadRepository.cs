using Payment.ReadApi.Queries;
using Nest;
namespace Payment.ReadApi.Infrastructure.Elastic
{
    public class TransactionReadRepository
    {
        private readonly IElasticClient _elastic;

        public TransactionReadRepository(IElasticClient elastic)
        {
            _elastic = elastic;
        }

        public async Task<TransactionDto?> GetByIdAsync(long id)
        {
            var response = await _elastic.GetAsync<TransactionDto>(id, g =>
                g.Index("transactions")
            );

            return response.Found ? response.Source : null;
        }
    }
}
