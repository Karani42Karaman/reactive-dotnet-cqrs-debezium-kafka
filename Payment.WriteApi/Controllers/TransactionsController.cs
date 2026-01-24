using Microsoft.AspNetCore.Mvc;
using Payment.WriteApi.Application.Commands;
using Prometheus;

namespace Payment.WriteApi.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly CreateTransactionHandler _handler;
    
    // Metrikleri static tanımla
    private static readonly Counter _transactionCounter = 
        Metrics.CreateCounter(
            "payment_transactions_total",
            "Total transactions",
            "status", "currency"
        );

    private static readonly Histogram _transactionAmount = 
        Metrics.CreateHistogram(
            "payment_transaction_amount",
            "Transaction amounts",
            "currency"
        );

    public TransactionsController(CreateTransactionHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTransactionCommand command)
    {
        try
        {
            var id = await _handler.Handle(command);

            // Metrikleri kaydet
            _transactionCounter
                .WithLabels("success", command.Currency)
                .Inc();

            _transactionAmount
                .WithLabels(command.Currency)
                .Observe((double)command.Amount);

            return Ok(new { Id = id });
        }
        catch (Exception ex)
        {
            _transactionCounter
                .WithLabels("failed", command.Currency)
                .Inc();
            throw;
        }
    }
}