$ErrorActionPreference = "Stop"
Set-Location -LiteralPath $PSScriptRoot
New-Item -ItemType Directory -Force -Path "data" | Out-Null
Write-Host "Iniciando servidor de feria en http://localhost:8080/"
Write-Host "URLs LAN detectadas para probar desde PCs cliente:"
$lanAddresses = @()
try {
    $lanAddresses = Get-NetIPAddress -AddressFamily IPv4 |
        Where-Object {
            $_.IPAddress -notlike "127.*" -and
            $_.IPAddress -notlike "169.254.*" -and
            $_.IPAddress -ne "0.0.0.0"
        } |
        Sort-Object InterfaceAlias, IPAddress
} catch {
    $lanAddresses = @()
}

if ($lanAddresses.Count -gt 0) {
    foreach ($address in $lanAddresses) {
        Write-Host ("  http://{0}:8080/health  ({1})" -f $address.IPAddress, $address.InterfaceAlias)
    }
} else {
    Write-Host "  No se detecto IPv4 LAN automaticamente. Ejecuta ipconfig y usa la IPv4 del host."
}

Write-Host "Si /health abre en el host pero no en clientes, permite TCP 8080 en Firewall de Windows."
Write-Host "Regla sugerida en PowerShell como administrador:"
Write-Host '  New-NetFirewallRule -DisplayName "Squid Fair Server 8080" -Direction Inbound -Protocol TCP -LocalPort 8080 -Action Allow'
python .\server.py --host 0.0.0.0 --port 8080 --db .\data\fair_server.sqlite3 --event-id feria-2026
