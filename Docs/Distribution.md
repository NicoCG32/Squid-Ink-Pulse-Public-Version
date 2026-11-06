# Distribución del juego

Las builds se adjuntan a [GitHub Releases](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases). El repositorio contiene las fuentes; los ZIP automáticos «Source code» contienen el proyecto de Unity.

## Descargas vigentes

| Plataforma | Release | Instalación |
| --- | --- | --- |
| Windows x64 | [v1.0.1 estable](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/windows-v1.0.1) | Extraer `SquidInkPulse-Windows-x64-v1.0.1.zip` y abrir `JUGAR.cmd`. |
| Android ARM64 | [v1.0.1 beta 1](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/android-v1.0.1-beta.1) | Instalar el APK en Android 7.1/API 25 o posterior. |

`JUGAR.cmd` inicia Windows con `--fair-disabled`. Mantener juntos el ejecutable, `SquidInkPulse_Data`, DLL y demás carpetas runtime. El equipo del jugador no necesita Unity instalado.

La beta Android usa el paquete `com.yecoworks.squidinkpulse`, versión `1.0.1`, versionCode `3` y firma Android Debug. Para actualizar sin desinstalar se necesita un certificado compatible. La futura candidata requiere firma de distribución y completar la [evaluación móvil](MobilePort.md).

El progreso es local. En Windows se guarda bajo `%USERPROFILE%/AppData/LocalLow/Yeco Works/Squid Ink-Pulse/db`; reemplazar la carpeta del ejecutable conserva esos datos. En Android, desinstalar puede eliminarlos. Respaldar el progreso antes de cambiar de firma o intentar instalar una versión anterior.

## Generar Windows

Con Unity 6000.3.11f1 y Windows Build Support, desde la raíz del proyecto:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.com' `
  -batchmode -nographics -quit -projectPath "$PWD" `
  -executeMethod WindowsReleaseBuilder.BuildWindowsRelease `
  -logFile "$PWD\TestResults\windows-release-build.log"
```

También está disponible en `Tools > Squid Ink Pulse > Build Windows Release`. La salida es `Build/WindowsRelease/`. El builder utiliza `BuildOptions.None`, valida las escenas y escribe `build-info.json`; conserva cualquier salida previa al rechazar una carpeta no vacía.

`Tools/PackageWindowsRelease.ps1` empaqueta el player, lanzador local, guía, créditos y procedencia. Excluye símbolos y helpers de reset/feria. Ejecutar las [pruebas y recorridos](03-desarrollo/pruebas-y-validacion.md) y comprobar el ZIP extraído antes de publicar.

## Generar Android

[AndroidBuild.md](AndroidBuild.md) reúne los comandos para APK Development y APK sin Development. La modalidad del player y su certificado son ajustes separados: un APK sin Development puede mantener firma Debug.

## Integridad y publicación

Cada entrega incluye notas, artefactos, `SHA256SUMS.txt`, `source-info.json` y `SOURCE.patch`. Las notas registran sus cambios y pruebas; compilar el código actual produce una build nueva.

Para comprobar una descarga en PowerShell:

```powershell
Get-FileHash .\SquidInkPulse-Windows-x64-v1.0.1.zip -Algorithm SHA256
```

Comparar con la línea del mismo archivo en `SHA256SUMS.txt`.

`Tools/PublishGithubRelease.ps1` sube primero a draft y comprueba tamaño y digest SHA-256. `-Publish` hace pública la release. Para retomar un draft se usa `-ReleaseId`; `-ReplaceDraftNotes` permite sustituir sus adjuntos de texto. Los binarios de una release publicada se conservan.

Windows se publica como release estable al completar su validación y Android como prerelease mientras siga en beta. Las versiones anteriores quedan disponibles para recuperación.
