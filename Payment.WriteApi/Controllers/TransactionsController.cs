using Microsoft.AspNetCore.Mvc;
using Payment.WriteApi.Application.Commands;

namespace Payment.WriteApi.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly CreateTransactionHandler _handler;

    public TransactionsController(CreateTransactionHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTransactionCommand command)
    {
        var id = await _handler.Handle(command);
        return Ok(new { Id = id });
    }
}
