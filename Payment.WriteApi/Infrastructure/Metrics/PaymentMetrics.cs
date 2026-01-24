using Prometheus;

namespace Payment.WriteApi.Infrastructure.Metrics;

public static class PaymentMetrics
{
    // Counter - Transaction sayısı
    public static readonly Counter TransactionCounter = 
        global::Prometheus.Metrics.CreateCounter(
            "payment_transactions_total",
            "Total number of transactions",
            "status", "currency"
        );

    // Histogram - Transaction miktarları
    public static readonly Histogram TransactionAmount = 
        global::Prometheus.Metrics.CreateHistogram(
            "payment_transaction_amount",
            "Transaction amounts in TRY",
            new HistogramConfiguration
            {
                LabelNames = new[] { "currency" },
                Buckets = Histogram.LinearBuckets(10, 100, 10)
            }
        );

    // Histogram - İşlem süreleri
    public static readonly Histogram TransactionDuration = 
        global::Prometheus.Metrics.CreateHistogram(
            "payment_transaction_duration_seconds",
            "Transaction processing duration",
            new HistogramConfiguration
            {
                LabelNames = new[] { "operation" },
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
            }
        );

    // Gauge - Aktif işlem sayısı
    public static readonly Gauge ActiveTransactions = 
        global::Prometheus.Metrics.CreateGauge(
            "payment_active_transactions",
            "Number of transactions being processed"
        );
}