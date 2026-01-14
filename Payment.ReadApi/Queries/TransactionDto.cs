namespace Payment.ReadApi.Queries
{
    public class TransactionDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
