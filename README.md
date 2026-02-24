# 💳 Payment CQRS — Gerçek Zamanlı Ödeme İşleme Sistemi

> **CQRS + Event Sourcing + CDC** prensiplerini kullanan, mikroservis tabanlı bir ödeme altyapısı.  
> Yazma ve okuma tarafları tamamen ayrılmış olup veri akışı **Debezium → Kafka → Elasticsearch** pipeline'ı üzerinden sağlanmaktadır.

---

## 📐 Mimari Genel Bakış

```
┌─────────────────────────────────────────────────────────────────────┐
│                          CLIENT / API CONSUMER                       │
└───────────────┬─────────────────────────────────┬───────────────────┘
                │ POST /api/transactions            │ GET /api/transactions/{id}
                ▼                                   ▼
  ┌─────────────────────────┐         ┌─────────────────────────┐
  │   Payment.WriteApi      │         │    Payment.ReadApi       │
  │   (Port: 6208)          │         │    (Port: 5289)          │
  │   ASP.NET Core 9        │         │    ASP.NET Core 9        │
  └────────────┬────────────┘         └────────────┬────────────┘
               │                                    │
               ▼                                    ▼
  ┌─────────────────────────┐         ┌─────────────────────────┐
  │   MSSQL Server 2022     │         │    Elasticsearch 8.12    │
  │   (Write DB)            │         │    (Read Store)          │
  └────────────┬────────────┘         └────────────┬────────────┘
               │                                    ▲
               │ CDC (Change Data Capture)           │ Index
               ▼                                    │
  ┌─────────────────────────┐         ┌─────────────────────────┐
  │   Debezium              │─Kafka──▶│  Payment.ReadConsumer   │
  │   (Kafka Connect)       │  Topic  │  (Background Worker)    │
  └─────────────────────────┘         └─────────────────────────┘
```

### Veri Akışı (Step by Step)

1. **Write:** İstemci, `POST /api/transactions` ile yeni bir ödeme işlemi oluşturur. WriteApi bunu doğrudan **MSSQL**'e kaydeder.
2. **CDC:** **Debezium**, MSSQL'in transaction log'unu (CDC) dinler ve her değişikliği otomatik olarak **Kafka**'ya publish eder.
3. **Consume:** **ReadConsumer** Kafka'dan mesajı tüketir, Debezium zarfını (envelope) parse eder ve **Elasticsearch**'e index'ler.
4. **Read:** İstemci, `GET /api/transactions/{id}` ile okuma yapar. ReadApi doğrudan **Elasticsearch**'ten döner.

Bu sayede okuma ve yazma tarafları birbirinden tamamen bağımsızdır; okuma tarafı asla MSSQL'e dokunmaz.

---

## 🗂 Proje Yapısı

```
Payment-CQRS/
├── Payment.WriteApi/               # Yazma API'si (MSSQL → Command tarafı)
│   ├── Application/Commands/       # CreateTransactionCommand + Handler
│   ├── Controllers/                # POST /api/transactions
│   ├── Domain/                     # Transaction entity
│   └── Infrastructure/
│       ├── Persistence/            # EF Core DbContext (MSSQL)
│       └── Metrics/                # Prometheus metrikleri
│
├── Payment.ReadConsumer/           # Kafka consumer (Arka plan servisi)
│   ├── Consumers/                  # TransactionConsumer (BackgroundService)
│   ├── Models/                     # TransactionReadModel
│   └── Infrastructure/
│       ├── Elastic/                # Elasticsearch index yönetimi
│       ├── Kafka/                  # DLQ (Dead Letter Queue) producer
│       └── Retry/                  # Exponential backoff retry policy
│
├── Payment.ReadApi/                # Okuma API'si (Elasticsearch → Query tarafı)
│   ├── Controllers/                # GET /api/transactions/{id}
│   ├── Queries/                    # TransactionDto + Query nesneleri
│   └── Infrastructure/
│       ├── Elastic/                # TransactionReadRepository
│       └── Monitoring/             # Prometheus metrikleri
│
├── docker/
│   ├── debezium/                   # Debezium connector konfigürasyonu
│   ├── mssql/                      # CDC aktifleştirme SQL scripti
│   ├── prometheus/                 # prometheus.yml, alerts.yml, alertmanager.yml
│   └── grafana/                    # Dashboard ve datasource provisioning
│
├── setup/setup.ps1                 # Tüm ortamı ayağa kaldıran setup scripti
├── fix-grafana.ps1                 # Grafana sorun giderme scripti
└── docker-compose.yml              # Tüm servisler
```

---

## 🛠 Teknoloji Yığını

| Katman | Teknoloji |
|---|---|
| Write API | ASP.NET Core 9, Entity Framework Core, MSSQL |
| CDC | Debezium 2.6 (SQL Server Connector) |
| Message Broker | Apache Kafka 7.6, Zookeeper |
| Read Consumer | .NET 9 Worker Service, Confluent.Kafka |
| Read Store | Elasticsearch 8.12, NEST client |
| Read API | ASP.NET Core 9 |
| Monitoring | Prometheus, Grafana, Alertmanager |
| Visualization | Kibana 8.12 |
| Containerization | Docker, Docker Compose |

---

## 🚀 Kurulum ve Çalıştırma

### Ön Gereksinimler

- Docker Desktop (v4+)
- PowerShell 7+ (kurulum scripti için)
- .NET 9 SDK (local geliştirme için, opsiyonel)

### Tek Komutla Kurulum

```powershell
cd setup
.\setup.ps1
```

Bu script sırasıyla şunları yapar:

1. Docker container'larını başlatır
2. MSSQL'de `payment` veritabanı ve `Transactions` tablosunu oluşturur
3. MSSQL üzerinde CDC'yi (database + table level) aktifleştirir
4. Debezium connector'ını Kafka Connect'e kaydeder
5. Connector durumunu doğrular
6. Kafka topic'lerini listeler

### Servislerin Manuel Başlatılması

```bash
docker-compose up -d
```

---

## 🌐 Servis Adresleri

| Servis | URL | Açıklama |
|---|---|---|
| Write API | http://localhost:6208 | Ödeme işlemi oluşturma |
| Read API | http://localhost:5289 | Ödeme işlemi sorgulama |
| Kafka UI | http://localhost:8080 | Kafka topic ve mesaj izleme |
| Kafka Connect | http://localhost:8083 | Debezium connector yönetimi |
| Elasticsearch | http://localhost:9200 | Read store |
| Kibana | http://localhost:5601 | ES görselleştirme |
| Prometheus | http://localhost:9090 | Metrik toplama |
| Grafana | http://localhost:3000 | Dashboard (admin/admin) |
| Alertmanager | http://localhost:9093 | Alert yönetimi |

---

## 📡 API Kullanımı

### Ödeme İşlemi Oluşturma (Write)

```http
POST http://localhost:6208/api/transactions
Content-Type: application/json

{
  "userId": 1,
  "amount": 250.75,
  "currency": "TRY"
}
```

**Yanıt:**
```json
{ "id": 1 }
```

### Ödeme İşlemi Sorgulama (Read)

```http
GET http://localhost:5289/api/transactions/1
```

**Yanıt:**
```json
{
  "id": 1,
  "userId": 1,
  "amount": 250.75,
  "currency": "TRY",
  "status": "CREATED",
  "createdAt": "2026-02-24T10:00:00Z"
}
```

### PowerShell ile Toplu Test

```powershell
# 20 adet test transaction oluştur
1..20 | ForEach-Object {
    Invoke-RestMethod `
        -Uri http://localhost:6208/api/transactions `
        -Method Post `
        -Body '{"userId":1,"amount":99.99,"currency":"TRY"}' `
        -ContentType "application/json"
}
```

---

## 📊 Monitoring ve Metrikler

Sistem iki katmanda Prometheus metrikleri üretir:

### Write API Metrikleri

| Metrik | Tür | Açıklama |
|---|---|---|
| `payment_transactions_total` | Counter | Toplam transaction sayısı (`status`, `currency` label'ları ile) |
| `payment_transaction_amount` | Histogram | Transaction tutarları dağılımı |
| `http_request_duration_seconds` | Histogram | HTTP istek süreleri |

### Read API Metrikleri

| Metrik | Tür | Açıklama |
|---|---|---|
| `payment_read_queries_total` | Counter | Toplam sorgu sayısı (`status`, `query_type` label'ları ile) |
| `payment_read_query_duration_seconds` | Histogram | Sorgu süreleri |
| `payment_elasticsearch_response_seconds` | Histogram | Elasticsearch yanıt süreleri |
| `payment_read_active_queries` | Gauge | Anlık aktif sorgu sayısı |

### Grafana Dashboard

`docker/grafana/provisioning/dashboards/payment-dashboard.json` dosyası otomatik olarak yüklenir ve şu panelleri içerir:

- **Total Transactions** — Toplam işlem sayısı
- **Transaction Rate** — Saniye başına işlem hızı
- **Success Rate** — Başarı oranı (%)
- **Avg Transaction Amount** — Ortalama işlem tutarı
- **Write API Response Time** — p50/p95/p99 latency
- **Read API & Elasticsearch Response Time** — Okuma katmanı performansı
- **Active Read Queries** — Anlık yük göstergesi
- **API Status** — Tüm servislerin up/down durumu
- **Error Rate (5xx)** — Hata oranı

### Alertmanager Kuralları

`docker/prometheus/alerts.yml` içinde tanımlı uyarılar:

- **HighErrorRate** — 5 dakikada hata oranı > 0.05 (critical)
- **SlowResponseTime** — p95 latency > 1s (warning)
- **APIDown** — Herhangi bir servis 1 dakika erişilemez olursa (critical)
- **HighKafkaLag** — Consumer lag > 1000 mesaj (warning)

---

## 🔄 Debezium CDC Detayları

Debezium, SQL Server'ın transaction log'unu okuyarak tablo değişikliklerini Kafka'ya iletir.

**Kafka Topic:** `dbserver1.payment.payment.dbo.Transactions`

**Mesaj Formatı (Debezium Envelope):**

```json
{
  "payload": {
    "op": "c",
    "before": null,
    "after": {
      "Id": 1,
      "UserId": 1,
      "Amount": 250.75,
      "Currency": "TRY",
      "Status": "CREATED"
    }
  }
}
```

| `op` Değeri | Anlamı |
|---|---|
| `c` | Create (INSERT) |
| `u` | Update (UPDATE) |
| `d` | Delete (DELETE) |
| `r` | Read (Snapshot) |

---

## ⚙️ Hata Yönetimi

### Dead Letter Queue (DLQ)

ReadConsumer, başarısız mesajları doğrudan atmak yerine `payment.transactions.dlq` topic'ine yönlendirir. Bu sayede veri kaybı önlenir ve başarısız mesajlar daha sonra incelenebilir.

### Retry Policy

Elasticsearch'e yazma işlemleri başarısız olduğunda **exponential backoff** ile 3 kez yeniden denenir:

- 1. deneme: 1000ms bekleme
- 2. deneme: 2000ms bekleme
- 3. deneme: Başarısız → DLQ'ya gönder

---

## 🔧 Sorun Giderme

### Grafana'da "No Data" Görünüyorsa

```powershell
.\fix-grafana.ps1
```

### Debezium Connector Durumu

```bash
curl http://localhost:8083/connectors/mssql-connector/status
```

### CDC Durumu Kontrolü

```bash
docker exec -i mssql /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'Karani123!' -C -d payment \
  -Q "SELECT name, is_tracked_by_cdc FROM sys.tables WHERE name = 'Transactions';"
```

### Container Log'larını İzleme

```bash
docker logs payment-read-consumer -f
docker logs kafka-connect -f
```

---

## 🏗 Geliştirme Ortamı

Local'de geliştirme yaparken `appsettings.Development.json` dosyaları `localhost` adreslerini kullanır. Docker ortamında ise servis isimleri (örn. `kafka`, `elasticsearch`, `mssql`) kullanılır.

| Servis | Local Port | Docker Internal |
|---|---|---|
| MSSQL | 1435 | 1433 |
| Kafka | 29092 | 9092 |
| Elasticsearch | 9200 | 9200 |

---

## 📋 CQRS Prensipleri

Bu proje CQRS'i şu şekilde uygular:

**Command (Yazma) tarafı** yalnızca veriyi doğrular ve MSSQL'e yazar. Okuma optimizasyonu veya sorgulama endişesi taşımaz.

**Query (Okuma) tarafı** yalnızca Elasticsearch'ten okur. MSSQL'e hiçbir zaman dokunmaz. Elasticsearch'in güçlü full-text ve aggregation yetenekleri sayesinde okuma performansı yazma tarafından bağımsız olarak ölçeklendirilebilir.

**Eventual Consistency:** Yazma ve okuma arasında kısa bir gecikme (tipik olarak milisaniyeler) oluşabilir. Bu CDC pipeline'ının doğal bir özelliğidir ve çoğu ödeme sistemi senaryosunda kabul edilebilirdir.# reactive-dotnet-cqrs-debezium-kafka
