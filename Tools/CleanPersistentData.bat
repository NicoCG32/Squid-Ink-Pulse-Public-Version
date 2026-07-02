@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0CleanPersistentData.ps1" %*
set EXIT_CODE=%ERRORLEVEL%
echo.
if not "%EXIT_CODE%"=="0" (
  echo No se pudo limpiar la persistencia. Revisa el mensaje anterior.
) else (
  echo Persistencia limpiada.
)
pause
exit /b %EXIT_CODE%
