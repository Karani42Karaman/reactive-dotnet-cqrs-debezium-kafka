namespace Payment.WriteApi.Domain;

public class Transaction
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Status { get; set; } = "CREATED";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
