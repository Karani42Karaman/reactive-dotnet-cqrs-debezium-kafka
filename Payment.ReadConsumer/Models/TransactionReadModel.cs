

namespace Payment.ReadConsumer.Models
{
    public class TransactionReadModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
