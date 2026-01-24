using Payment.ReadApi.Queries;
using Payment.ReadApi.Infrastructure.Monitoring;
using Nest;

namespace Payment.ReadApi.Infrastructure.Elastic;

public class TransactionReadRepository
{
    private readonly IElasticClient _elastic;
    private readonly ILogger<TransactionReadRepository> _logger;

    public TransactionReadRepository(
        IElasticClient elastic,
        ILogger<TransactionReadRepository> logger)
    {
        _elastic = elastic;
        _logger = logger;
    }

    public async Task<TransactionDto?> GetByIdAsync(long id)
    {
        try
        {
            var response = await _elastic.GetAsync<TransactionDto>(id, g =>
                g.Index("transactions")
            );

            if (!response.IsValid)
            {
                _logger.LogError(
                    "Elasticsearch error: {Error}", 
                    response.OriginalException?.Message);
            }

            return response.Found ? response.Source : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elasticsearch query failed for ID: {Id}", id);
            throw;
        }
    }
}