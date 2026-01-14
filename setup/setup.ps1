Write-Host "Starting CQRS Payment System Setup..." -ForegroundColor Green

# 1. Docker servisleri baslat
Write-Host "Starting Docker services..." -ForegroundColor Cyan
# Docker containerları durdur ve sil
docker-compose down -v

# Tüm containerları sil
docker rm -f $(docker ps -aq)

# Tüm volume'ları sil
docker volume rm $(docker volume ls -q)

# Tüm networkları temizle (default olanlar kalacak)
docker network prune -f
# Docker containerları baslat
docker-compose up -d

Write-Host "Waiting for services to be healthy..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# 2. Database olustur
Write-Host "Creating database..." -ForegroundColor Cyan
docker exec -i mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Karani123!' -C -Q "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'payment') BEGIN CREATE DATABASE payment; END"

Start-Sleep -Seconds 5

# 3. Table olustur
Write-Host "Creating Transactions table..." -ForegroundColor Cyan
$createTableQuery = "IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Transactions') BEGIN CREATE TABLE Transactions ( Id BIGINT PRIMARY KEY IDENTITY(1,1), UserId BIGINT NOT NULL, Amount DECIMAL(18,2) NOT NULL, Currency NVARCHAR(10) NOT NULL, Status NVARCHAR(50) NOT NULL, CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE() ); END"

docker exec -i mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Karani123!' -C -d payment -Q $createTableQuery

Start-Sleep -Seconds 5

# 4. CDC'yi aktif et - Database level
Write-Host "Enabling CDC on database..." -ForegroundColor Cyan
docker exec -i mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Karani123!' -C -d payment -Q "ALTER DATABASE payment SET RECOVERY FULL; EXEC sys.sp_cdc_enable_db;"

Start-Sleep -Seconds 3

# 5. CDC'yi aktif et - Table level
Write-Host "Enabling CDC on table..." -ForegroundColor Cyan
$cdcTableQuery = "IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Transactions' AND is_tracked_by_cdc = 1) BEGIN EXEC sys.sp_cdc_enable_table @source_schema = N'dbo', @source_name = N'Transactions', @role_name = NULL, @supports_net_changes = 1; END"

docker exec -i mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Karani123!' -C -d payment -Q $cdcTableQuery

Start-Sleep -Seconds 10

# 6. CDC durumunu kontrol et
Write-Host "Checking CDC status..." -ForegroundColor Cyan
docker exec -i mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Karani123!' -C -d payment -Q "SELECT name, is_cdc_enabled FROM sys.databases WHERE name = 'payment'; SELECT name, is_tracked_by_cdc FROM sys.tables WHERE name = 'Transactions';"

# 7. Debezium connector'i kaydet
Write-Host "Registering Debezium connector..." -ForegroundColor Cyan
Start-Sleep -Seconds 5

# Once varsa sil
try {
    Invoke-RestMethod -Uri http://localhost:8083/connectors/mssql-connector -Method Delete -ErrorAction SilentlyContinue | Out-Null
} catch {
    Write-Host "No existing connector to delete" -ForegroundColor Yellow
}

# JSON icerigi
$connectorConfig = @'
{
  "name": "mssql-connector",
  "config": {
    "connector.class": "io.debezium.connector.sqlserver.SqlServerConnector",
    "tasks.max": "1",
    "database.hostname": "mssql",
    "database.port": "1433",
    "database.user": "sa",
    "database.password": "Karani123!",
    "database.names": "payment",
    "database.server.name": "dbserver1",
    "table.include.list": "dbo.Transactions",
    "topic.prefix": "dbserver1.payment",
    "snapshot.mode": "initial",
    "snapshot.isolation.mode": "read_committed",
    "database.encrypt": "false",
    "database.trustServerCertificate": "true",
    "schema.history.internal.kafka.bootstrap.servers": "kafka:9092",
    "schema.history.internal.kafka.topic": "schema-changes.payment",
    "key.converter": "org.apache.kafka.connect.json.JsonConverter",
    "value.converter": "org.apache.kafka.connect.json.JsonConverter",
    "key.converter.schemas.enable": "true",
    "value.converter.schemas.enable": "true",
    "decimal.handling.mode": "double",
    "time.precision.mode": "adaptive_time_microseconds"
  }
}
'@

# Connector'i kaydet
try {
    $response = Invoke-RestMethod -Uri http://localhost:8083/connectors -Method Post -Body $connectorConfig -ContentType "application/json"
    Write-Host "Connector registered successfully!" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 10
} catch {
    Write-Host "Failed to register connector: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Error details: $($_.ErrorDetails.Message)" -ForegroundColor Red
}

Start-Sleep -Seconds 5

# 8. Connector durumunu kontrol et
Write-Host "Checking connector status..." -ForegroundColor Cyan
try {
    $status = Invoke-RestMethod -Uri http://localhost:8083/connectors/mssql-connector/status
    Write-Host "Connector State: $($status.connector.state)" -ForegroundColor $(if($status.connector.state -eq "RUNNING"){"Green"}else{"Red"})
    $status | ConvertTo-Json -Depth 10
} catch {
    Write-Host "Failed to get connector status: $($_.Exception.Message)" -ForegroundColor Red
}

# 9. Kafka topics listele
Write-Host ""
Write-Host "Listing Kafka topics..." -ForegroundColor Cyan
docker exec kafka kafka-topics --list --bootstrap-server localhost:9092

Write-Host ""
Write-Host "Setup completed!" -ForegroundColor Green
Write-Host ""
Write-Host "Important URLs:" -ForegroundColor Yellow
Write-Host "   - Kafka UI: http://localhost:8080"
Write-Host "   - Kibana: http://localhost:5601"
Write-Host "   - Prometheus: http://localhost:9090"
Write-Host "   - Kafka Connect: http://localhost:8083"
Write-Host ""
Write-Host "Test with:" -ForegroundColor Yellow
Write-Host '   Invoke-RestMethod -Uri http://localhost:6208/api/transactions -Method Post -Body ''{"userId":1,"amount":100.50,"currency":"TRY"}'' -ContentType "application/json"'


pause