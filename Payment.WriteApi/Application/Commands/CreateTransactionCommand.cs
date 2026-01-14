namespace Payment.WriteApi.Application.Commands;

public record CreateTransactionCommand(
    long UserId,
    decimal Amount,
    string Currency
);
