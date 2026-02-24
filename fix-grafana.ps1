Write-Host "=== Grafana No Data Sorunu - Hızlı Çözüm ===" -ForegroundColor Cyan
Write-Host ""

# 1. Servisleri durdur
Write-Host "1. Servisleri durduruyor..." -ForegroundColor Yellow
docker-compose down
Start-Sleep -Seconds 5

# 2. Yeniden başlat
Write-Host "2. Servisleri başlatıyor..." -ForegroundColor Yellow
docker-compose up -d
Write-Host "Servislerin hazır olması bekleniyor (30 saniye)..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# 3. Durum kontrolü
Write-Host ""
Write-Host "3. Container durumları:" -ForegroundColor Yellow
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | Select-String -Pattern "prometheus|grafana|payment"

# 4. Prometheus targets kontrolü
Write-Host ""
Write-Host "4. Prometheus Targets kontrolü:" -ForegroundColor Yellow
Start-Sleep -Seconds 5
try {
    $targets = Invoke-RestMethod -Uri http://localhost:9090/api/v1/targets -ErrorAction Stop
    $targets.data.activeTargets | ForEach-Object {
        $status = if($_.health -eq "up") { "✅ UP" } else { "❌ DOWN" }
        Write-Host "$status - $($_.labels.job)" -ForegroundColor $(if($_.health -eq "up"){"Green"}else{"Red"})
    }
} catch {
    Write-Host "❌ Prometheus'a erişilemiyor! Lütfen bekleyin ve tekrar deneyin." -ForegroundColor Red
    Write-Host "   URL: http://localhost:9090/targets" -ForegroundColor Gray
    exit 1
}

# 5. Metrics endpoint kontrolü
Write-Host ""
Write-Host "5. API Metrics endpoint kontrolü:" -ForegroundColor Yellow
@{
    "Write API" = "6208"
    "Read API" = "5289"
}.GetEnumerator() | ForEach-Object {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$($_.Value)/metrics" -TimeoutSec 3 -ErrorAction Stop
        Write-Host "✅ $($_.Key) (Port $($_.Value))" -ForegroundColor Green
    } catch {
        Write-Host "❌ $($_.Key) (Port $($_.Value)) - Erişilemiyor" -ForegroundColor Red
    }
}

# 6. Test transaction'ları oluştur
Write-Host ""
Write-Host "6. Test transaction'ları oluşturuluyor..." -ForegroundColor Yellow
$successCount = 0
$failCount = 0

1..5 | ForEach-Object {
    try {
        $body = @{
            userId = Get-Random -Minimum 1 -Maximum 100
            amount = [Math]::Round((Get-Random -Minimum 10 -Maximum 1000) + (Get-Random), 2)
            currency = "TRY"
        } | ConvertTo-Json
        
        $result = Invoke-RestMethod -Uri http://localhost:6208/api/transactions -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop
        $successCount++
        Write-Host "  ✅ Transaction $_/5 created (ID: $($result.Id))" -ForegroundColor Green
    } catch {
        $failCount++
        Write-Host "  ❌ Transaction $_/5 failed" -ForegroundColor Red
    }
    Start-Sleep -Milliseconds 200
}

Write-Host ""
Write-Host "Transaction sonuçları: $successCount başarılı, $failCount başarısız" -ForegroundColor $(if($successCount -eq 5){"Green"}else{"Yellow"})

# 7. Metrikleri kontrol et
Write-Host ""
Write-Host "7. Prometheus'ta metrikleri kontrol ediyor..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

$metrics = @(
    "up",
    "payment_transactions_total",
    "payment_read_queries_total",
    "http_requests_received_total"
)

foreach ($metric in $metrics) {
    try {
        $result = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/query?query=$metric" -ErrorAction Stop
        $resultCount = $result.data.result.Count
        
        if ($resultCount -gt 0) {
            Write-Host "  ✅ $metric - $resultCount sonuç" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️  $metric - Veri yok (henüz oluşmamış olabilir)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  ❌ $metric - Query başarısız" -ForegroundColor Red
    }
}

# 8. Grafana datasource kontrolü
Write-Host ""
Write-Host "8. Grafana datasource test ediliyor..." -ForegroundColor Yellow
Start-Sleep -Seconds 2

try {
    # Grafana API ile datasource test et
    $auth = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes("admin:admin"))
    $headers = @{
        "Authorization" = "Basic $auth"
        "Content-Type" = "application/json"
    }
    
    # Datasource listesini al
    $datasources = Invoke-RestMethod -Uri "http://localhost:3000/api/datasources" -Headers $headers -ErrorAction Stop
    $promDs = $datasources | Where-Object { $_.type -eq "prometheus" }
    
    if ($promDs) {
        Write-Host "  ✅ Prometheus datasource bulundu: $($promDs.name)" -ForegroundColor Green
        
        # Datasource'u test et
        try {
            $testResult = Invoke-RestMethod -Uri "http://localhost:3000/api/datasources/$($promDs.id)/health" -Headers $headers -ErrorAction Stop
            Write-Host "  ✅ Datasource sağlık kontrolü başarılı" -ForegroundColor Green
        } catch {
            Write-Host "  ⚠️  Datasource test edilemedi - Manuel kontrol edin" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  ❌ Prometheus datasource bulunamadı" -ForegroundColor Red
    }
} catch {
    Write-Host "  ⚠️  Grafana API'ye erişilemiyor - Henüz hazır olmayabilir" -ForegroundColor Yellow
}

# 9. Özet
Write-Host ""
Write-Host "=== Kontrol Tamamlandı ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 Grafana Dashboard: http://localhost:3000" -ForegroundColor White
Write-Host "   Username: admin" -ForegroundColor Gray
Write-Host "   Password: admin" -ForegroundColor Gray
Write-Host ""
Write-Host "📈 Prometheus: http://localhost:9090" -ForegroundColor White
Write-Host "   Targets: http://localhost:9090/targets" -ForegroundColor Gray
Write-Host "   Graph: http://localhost:9090/graph" -ForegroundColor Gray
Write-Host ""

if ($successCount -gt 0) {
    Write-Host "✅ Sistem çalışıyor! Grafana'da dashboard'u açın." -ForegroundColor Green
    Write-Host ""
    Write-Host "Dashboard'u import etmek için:" -ForegroundColor Yellow
    Write-Host "  1. Grafana'ya giriş yapın (http://localhost:3000)" -ForegroundColor Gray
    Write-Host "  2. Dashboard → Import" -ForegroundColor Gray
    Write-Host "  3. docker/grafana/provisioning/dashboards/payment-dashboard.json dosyasını yükleyin" -ForegroundColor Gray
} else {
    Write-Host "⚠️  Bazı sorunlar var. Lütfen yukarıdaki hataları kontrol edin." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Detaylı troubleshooting için TROUBLESHOOTING.md dosyasına bakın." -ForegroundColor Gray
}

Write-Host ""
Write-Host "Daha fazla test transaction oluşturmak için:" -ForegroundColor Yellow
Write-Host '  1..20 | % { Invoke-RestMethod -Uri http://localhost:6208/api/transactions -Method Post -Body ''{"userId":1,"amount":99.99,"currency":"TRY"}'' -ContentType "application/json" }' -ForegroundColor Gray

Write-Host ""