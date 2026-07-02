@echo off
setlocal
cd /d "%~dp0"
if not exist data mkdir data
echo Iniciando servidor de feria en http://localhost:8080/
echo URLs LAN detectadas para probar desde PCs cliente:
powershell -NoProfile -ExecutionPolicy Bypass -Command "try { $ips = Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*' -and $_.IPAddress -ne '0.0.0.0' } | Sort-Object InterfaceAlias, IPAddress; if ($ips) { $ips | ForEach-Object { '  http://{0}:8080/health  ({1})' -f $_.IPAddress, $_.InterfaceAlias } } else { '  No se detecto IPv4 LAN automaticamente. Ejecuta ipconfig y usa la IPv4 del host.' } } catch { '  No se pudo consultar la IPv4 LAN. Ejecuta ipconfig y usa la IPv4 del host.' }"
echo Si /health abre en el host pero no en clientes, permite TCP 8080 en Firewall de Windows.
echo Regla sugerida en PowerShell como administrador:
echo   New-NetFirewallRule -DisplayName "Squid Fair Server 8080" -Direction Inbound -Protocol TCP -LocalPort 8080 -Action Allow
echo.
py -3 server.py --host 0.0.0.0 --port 8080 --db data\fair_server.sqlite3 --event-id feria-2026
if errorlevel 1 (
  echo.
  echo No se pudo iniciar el servidor. Revisa que Python 3 este instalado.
  pause
)
