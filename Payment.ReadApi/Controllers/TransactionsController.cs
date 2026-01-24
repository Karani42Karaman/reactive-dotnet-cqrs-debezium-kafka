using Microsoft.AspNetCore.Mvc;
using Payment.ReadApi.Infrastructure.Elastic;
using Payment.ReadApi.Infrastructure.Monitoring;
using Prometheus;
using System.Diagnostics;

namespace Payment.ReadApi.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly TransactionReadRepository _repo;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        TransactionReadRepository repo,
        ILogger<TransactionsController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(long id)
    {
        // Aktif sorgu sayısını artır
        ReadMetrics.ActiveQueries.Inc();

        // Query süresini ölç
        using (ReadMetrics.QueryDuration.WithLabels("get_by_id").NewTimer())
        {
            try
            {
                // Elasticsearch response time için stopwatch
                var sw = Stopwatch.StartNew();
                var result = await _repo.GetByIdAsync(id);
                sw.Stop();

                // Elasticsearch response time kaydet
                ReadMetrics.ElasticsearchResponseTime.Observe(sw.Elapsed.TotalSeconds);

                if (result is null)
                {
                    // Not found metriği
                    ReadMetrics.QueryCounter
                        .WithLabels("not_found", "get_by_id")
                        .Inc();

                    _logger.LogWarning("Transaction not found. ID: {Id}", id);

                    return NotFound();
                }

                // Success metriği
                ReadMetrics.QueryCounter
                    .WithLabels("success", "get_by_id")
                    .Inc();

                _logger.LogInformation("Transaction retrieved successfully. ID: {Id}", id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Error metriği
                ReadMetrics.QueryCounter
                    .WithLabels("failed", "get_by_id")
                    .Inc();

                _logger.LogError(ex, "Query failed for ID: {Id}", id);
                
                return StatusCode(500, new { error = "Internal server error" });
            }
            finally
            {
                // Aktif sorgu sayısını azalt
                ReadMetrics.ActiveQueries.Dec();
            }
        }
    }
}