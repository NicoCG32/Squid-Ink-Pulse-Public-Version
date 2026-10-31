# Distribución del juego

Windows y Android se distribuyen por separado en [GitHub Releases](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases). Los binarios se adjuntan a una release; no se agregan al repositorio. Los archivos automáticos `Source code` contienen el proyecto, no el juego compilado.

## Windows x64

El 9 de octubre de 2026 se publicó [Windows v1.0.0](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/windows-v1.0.0) como release estable y descarga principal. ZIP `SquidInkPulse-Windows-x64-v1.0.0.zip`: 345.942.054 bytes; SHA-256 `e3f131724b199d3683b78598e896f1993c1670836328b6d100db6b37ed422d26`. Fuente de build: `dbc26f337d0373575e304f1fabd476c3e0f8e73a`; Unity 6000.3.11f1, `BuildOptions.None`, 0 errores y 2 advertencias. Pasaron 255 EditMode y 5 PlayMode; composición aprobada. Se extrajo el ZIP, comparó el runtime con el build por hash y observó MainMenu. La validación de escritorio comprende una partida, Ink-Pulse, gadgets, pausa, portales, Game Over y reintento, opciones, tiendas y progreso tras reiniciar.

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

[Android v1.0.0 beta 2](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/android-v1.0.0-beta.2) distribuye un APK ARM64 sin Development ni AllowDebugging. Requiere Android 7.1/API 25 o superior, con target API 36. Versión interna `1.0`, versionCode `2`, package `com.yecoworks.squidinkpulse`. Descargar `SquidInkPulse-Android-arm64-v1.0.0-beta.2.apk`, abrirlo en Android y autorizar esa fuente de instalación si el sistema lo solicita. El ZIP `Source code` no es instalable.

La beta conserva la firma Android Debug de beta 1, con huella pública SHA-256 `ce82237f47c94b4898382c4c4c7562a03e860e719ab5d08110741559f8345a86`. Permite actualizar una instalación compatible sin desinstalar; la actualización comprobada en Redmi Pad Pro conservó los hashes de perfil, récords y catálogo. Esta revisión no repitió una instalación limpia. No es una entrega de Google Play: la futura candidata requiere firma de distribución y su gate móvil. Un cambio de certificado puede exigir reinstalación; respaldar el progreso antes de cambiar de firma o intentar volver a un versionCode menor. Véase [firma de aplicaciones Android](https://developer.android.com/studio/publish/app-signing).

La beta 2 incorpora controles y menús móviles, pausa/Game Over, opciones, tiendas, cómics con áreas seguras, exclusión entre overlays y gameplay e identidad visual. Solicita 60 FPS sin reducir calidad gráfica; el menú del APK publicado presentó aproximadamente 60 FPS en una muestra corta de Redmi Pad Pro. El objetivo no garantiza 60 sostenidos en todos los teléfonos. Siguen pendientes rendimiento prolongado, Abisopelágica bajo carga, más hardware, Android Back global, suspensión y orientación opuesta. Véase [medición y límites](AndroidPerformance.md).

Fuente de build `a9e389140631eed1204b0a09169fabccf2497258`, Unity 6000.3.11f1, `BuildOptions.None`; APK de 351.753.363 bytes, SHA-256 `e61ae640284a3450209f676d76bde820d83e4114587e017c005a578897c20145`. La release adjunta `source-info.json` y `SOURCE.patch` con las diferencias de serialización de Unity y los alcances de validación. La [beta 1 Development](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/android-v1.0.0-beta.1) se conserva como entrega histórica.

El APK, su procedencia y SHA-256 se publican juntos. No incluye keystore, claves, capturas personales ni logs del equipo. [AndroidBuild.md](AndroidBuild.md) conserva los comandos y la evidencia de construcción/instalación.

## Integridad y publicación

Cada release incluye notas, referencia de fuente, artefactos y `SHA256SUMS.txt`. Para comprobar una descarga en PowerShell:

```powershell
Get-FileHash .\SquidInkPulse-Windows-x64-v1.0.0.zip -Algorithm SHA256
```

Comparar con la línea del mismo archivo en `SHA256SUMS.txt`. GitHub limita cada adjunto a menos de 2 GiB; los paquetes deben comprobarse antes de subir. Véase [documentación de GitHub Releases](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases).

Windows se publica como release normal sólo al completar su validación; mientras tanto puede conservarse como draft. Android permanece marcada como prerelease para que no sustituya a Windows como descarga estable más reciente. Conservar las releases anteriores para recuperación.

`Tools/PackageWindowsRelease.ps1` empaqueta el player, lanzador local, guía/créditos y procedencia; excluye símbolos y helpers de reset/feria. `Tools/PublishGithubRelease.ps1` sube primero a draft y comprueba tamaño y digest SHA-256 del servidor. Sólo `-Publish` hace pública la release. No imprime ni guarda credenciales; reutiliza Git Credential Manager. Para retomar un draft, usar `-ReleaseId` porque GitHub puede asignarle una URL/tag temporal `untagged-*`. `-ReplaceDraftNotes` permite sustituir únicamente adjuntos de texto de un draft cuando se actualiza su evidencia; nunca reemplaza binarios ni assets de una release ya publicada.
