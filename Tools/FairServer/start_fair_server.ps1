$ErrorActionPreference = "Stop"
Set-Location -LiteralPath $PSScriptRoot
New-Item -ItemType Directory -Force -Path "data" | Out-Null
Write-Host "Iniciando servidor de feria en http://localhost:8080/"
Write-Host "Para LAN, usa la IP del PC host con puerto 8080."
python .\server.py --host 0.0.0.0 --port 8080 --db .\data\fair_server.sqlite3 --event-id feria-2026
