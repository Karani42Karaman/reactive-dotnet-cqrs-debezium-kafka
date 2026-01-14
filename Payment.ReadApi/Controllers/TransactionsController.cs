using Microsoft.AspNetCore.Mvc;
using Payment.ReadApi.Infrastructure.Elastic;
using Payment.ReadApi.Queries;

namespace Payment.ReadApi.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    public class TransactionsController : ControllerBase
    {
        private readonly TransactionReadRepository _repo;

        public TransactionsController(TransactionReadRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var result = await _repo.GetByIdAsync(id);

            return result is null
                ? NotFound()
                : Ok(result);
        }
    }
}
