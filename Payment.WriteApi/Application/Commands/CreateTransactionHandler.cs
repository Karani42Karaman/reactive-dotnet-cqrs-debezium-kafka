using Payment.WriteApi.Domain;
using Payment.WriteApi.Infrastructure.Persistence;

namespace Payment.WriteApi.Application.Commands;

public class CreateTransactionHandler
{
    private readonly WriteDbContext _db;

    public CreateTransactionHandler(WriteDbContext db)
    {
        _db = db;
    }

    public async Task<long> Handle(CreateTransactionCommand command)
    {
        var tx = new Transaction
        {
            UserId = command.UserId,
            Amount = command.Amount,
            Currency = command.Currency
        };

        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync();

        return tx.Id;
    }
}
