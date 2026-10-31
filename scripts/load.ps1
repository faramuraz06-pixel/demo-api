# Фоновая нагрузка на DemoApi для лабораторных по мониторингу. Остановка: Ctrl+C
# Запуск: pwsh ./scripts/load.ps1
# Строки вывода латиницей: так скрипт работает и в Windows PowerShell 5.1
param(
    [string]$BaseUrl = 'http://localhost:8080',
    [int]$DelayMs = 300
)

$json = 'application/json'
$project = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/projects" -ContentType $json `
    -Body (@{ name = "load-$(Get-Date -Format 'yyyyMMdd-HHmmss')" } | ConvertTo-Json)
Write-Host "Project $($project.id). Load on $BaseUrl, press Ctrl+C to stop"

$i = 0
while ($true) {
    $i++
    try {
        $task = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/tasks" -ContentType $json `
            -Body (@{ projectId = $project.id; title = "Load task $i" } | ConvertTo-Json)
        Invoke-RestMethod -Method Patch -Uri "$BaseUrl/api/tasks/$($task.id)/status" -ContentType $json `
            -Body (@{ status = 'InProgress' } | ConvertTo-Json) | Out-Null
        Invoke-RestMethod -Uri "$BaseUrl/api/tasks?projectId=$($project.id)&status=Todo" | Out-Null
        if ($i % 20 -eq 0) { Write-Host "iterations: $i" }
    }
    catch {
        Write-Host "error: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    Start-Sleep -Milliseconds $DelayMs
}
