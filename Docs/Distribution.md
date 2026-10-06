# Distribución del juego

Windows y Android se distribuyen por separado en [GitHub Releases](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases). Los binarios se adjuntan a una release; no se agregan al repositorio. Los archivos automáticos `Source code` contienen el proyecto, no el juego compilado.

## Windows x64

El paquete contiene la carpeta completa del player y un lanzador `JUGAR.cmd`. Descomprimir el ZIP y abrir ese lanzador desde la carpeta extraída; inicia el juego local con `--fair-disabled`, sin buscar un servidor de feria. Mantener juntos el ejecutable, `SquidInkPulse_Data`, DLL y demás carpetas runtime. No requiere Unity instalado.

Controles: mouse para dirigir al jugador; click o Espacio para Ink-Pulse; P/Escape para pausa; Q/W para los gadgets; B para comprar en la tienda de run. Menús y comics usan mouse. El comic de `Cómo Jugar` está disponible desde el menú principal.

El progreso se guarda localmente bajo `%USERPROFILE%/AppData/LocalLow/Yeco Works/Squid Ink-Pulse/db`. El ZIP incluye semillas del juego, nunca el perfil real de quien compiló. Sustituir la carpeta del ejecutable no reinicia el progreso. Cerrar el juego y respaldar la carpeta de datos antes de intentar restaurar una versión anterior.

### Generar un player de distribución

Usar Unity 6000.3.11f1 con Windows Build Support, las cuatro escenas canónicas y una fuente identificada por commit. Preservar los cambios locales ajenos utilizando un checkout aislado.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.com' `
  -batchmode -nographics -quit -projectPath "$PWD" `
  -executeMethod WindowsReleaseBuilder.BuildWindowsRelease `
  -logFile "$PWD\TestResults\windows-release-build.log"
```

También está en `Tools > Squid Ink Pulse > Build Windows Release`. La salida es `Build/WindowsRelease/`. El builder utiliza `BuildOptions.None`, rechaza Development/AllowDebugging en el reporte y escribe `build-info.json`. Si la carpeta ya contiene un artefacto, falla para conservarlo.

Antes de etiquetar una release como estable: ejecutar EditMode/PlayMode, validar composición, compilar, extraer el ZIP y comprobar menú, tutorial gráfico, movimiento, graze/Ink-Pulse, gadgets/pausa, portal entre ambas zonas, muerte/Game Over/retry, opciones/tiendas y persistencia al reiniciar. Registrar exactamente qué se comprobó; un smoke de arranque no certifica el recorrido completo.

## Android beta

[Beta inicial](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/android-v1.0.0-beta.1): APK Development para pruebas directas, ARM64, Android 7.1/API 25 o superior. Versión interna `1.0`, versionCode `1`, package `com.yecoworks.squidinkpulse`. Descargar el adjunto `.apk`, abrirlo en el teléfono y autorizar esa fuente de instalación si Android lo solicita. El ZIP `Source code` no es instalable.

Esta beta conserva debugging y firma Android Debug. Es una prerelease de prueba directa; no una entrega de Google Play. La futura distribución final requiere firma de release y su gate móvil. Android exige la misma identidad de firma para actualizar una instalación; si se cambia de certificado, puede ser necesario reinstalar y respaldar primero el progreso. Véase [firma de aplicaciones Android](https://developer.android.com/studio/publish/app-signing).

La beta inicial corresponde al APK del 1 de octubre de 2026, verificado en POCO X6 5G/Android 16: controles y HUD aceptados en ambas zonas. El código corresponde al cierre de cámara `3fe6789`, construido antes de registrar el commit, con la serialización local de `TimeManager.asset` documentada en un parche de procedencia. Conserva pendientes de opciones/menús, bandas del fondo de tienda, overlays, ciclo de vida/Back, orientación opuesta, rendimiento sostenido y pruebas en más hardware. No garantiza esos casos.

El APK, su procedencia y SHA-256 se publican juntos. No incluye keystore, claves, capturas personales ni logs del equipo. [AndroidBuild.md](AndroidBuild.md) conserva los comandos y la evidencia de construcción/instalación.

## Integridad y publicación

Cada release incluye notas, referencia de fuente, artefactos y `SHA256SUMS.txt`. Para comprobar una descarga en PowerShell:

```powershell
Get-FileHash .\SquidInkPulse-Windows-x64-v1.0.0.zip -Algorithm SHA256
```

Comparar con la línea del mismo archivo en `SHA256SUMS.txt`. GitHub limita cada adjunto a menos de 2 GiB; los paquetes deben comprobarse antes de subir. Véase [documentación de GitHub Releases](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases).

Windows se publica como release normal sólo al completar su validación; mientras tanto puede conservarse como draft. Android permanece marcada como prerelease para que no sustituya a Windows como descarga estable más reciente. Conservar las releases anteriores para recuperación.
