@echo off
setlocal
cd /d "%~dp0"
if not exist data mkdir data
py -3 server.py --host 0.0.0.0 --port 8080 --db data\fair_server.sqlite3 --event-id feria-2026
if errorlevel 1 (
  echo.
  echo No se pudo iniciar el servidor. Revisa que Python 3 este instalado.
  pause
)
