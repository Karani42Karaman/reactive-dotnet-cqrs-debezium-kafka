using Prometheus;

namespace Payment.ReadApi.Infrastructure.Monitoring;

public static class ReadMetrics
{
    // Counter - Query sayısı
    public static readonly Counter QueryCounter = 
        global::Prometheus.Metrics.CreateCounter(
            "payment_read_queries_total",
            "Total number of read queries",
            "status", "query_type"
        );

    // Histogram - Query süreleri
    public static readonly Histogram QueryDuration = 
        global::Prometheus.Metrics.CreateHistogram(
            "payment_read_query_duration_seconds",
            "Query execution duration",
            new HistogramConfiguration
            {
                LabelNames = new[] { "query_type" },
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 10) // 1ms'den başla
            }
        );

    // Histogram - Elasticsearch response time
    public static readonly Histogram ElasticsearchResponseTime = 
        global::Prometheus.Metrics.CreateHistogram(
            "payment_elasticsearch_response_seconds",
            "Elasticsearch response time",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
            }
        );

    // Gauge - Aktif sorgular
    public static readonly Gauge ActiveQueries = 
        global::Prometheus.Metrics.CreateGauge(
            "payment_read_active_queries",
            "Number of queries being processed"
        );

    // Counter - Cache hit/miss (gelecekte eklenebilir)
    public static readonly Counter CacheCounter = 
        global::Prometheus.Metrics.CreateCounter(
            "payment_read_cache_total",
            "Cache hit/miss statistics",
            "result" // hit, miss
        );
}