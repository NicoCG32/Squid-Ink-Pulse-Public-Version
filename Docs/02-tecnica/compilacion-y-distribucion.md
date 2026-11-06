# Compilación y distribución

[← Área técnica](README.md)

## Releases publicados

| Sistema | Versión | Estado |
| --- | --- | --- |
| Windows x64 | [v1.0.1](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/windows-v1.0.1) | Estable. |
| Android ARM64 | [v1.0.1 beta 1](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/android-v1.0.1-beta.1) | Beta, con firma de desarrollo. |

Ambas versiones se publicaron el **31 de octubre de 2026**. Windows 1.0.1 actualiza el icono y acumula ajustes visuales; Android 1.0.1 beta 1 actualiza iconos adaptativos y mantiene la validación móvil pendiente. Las notas de release distinguen expresamente **las pruebas ejecutadas para ese corte** de las suites del runtime anterior. No convertimos una prueba anterior en una certificación nueva.

Los adjuntos `SHA256SUMS.txt`, `source-info.json` y `SOURCE.patch` permiten comprobar integridad y procedencia. El botón automático «Source code» no descarga el ejecutable ni el APK.

## Abrir el proyecto

1. Instalar Unity Hub y el editor **6000.3.11f1** (según `ProjectSettings/ProjectVersion.txt`).
2. Importar el repositorio como proyecto existente.
3. Abrir `Assets/Scenes/MainMenu/MainMenu.unity`.
4. Revisar errores de consola y referencias antes de iniciar Play Mode.

Las cuatro escenas activas de `ProjectSettings/EditorBuildSettings.asset` son `MainMenu`, `ZonaEpipelagica`, `ZonaAbisopelagica` y `ShopMenu`. El tutorial jugable aislado queda fuera del build.

## Generación de Windows

El proyecto incluye `WindowsReleaseBuilder` en `Assets/Implementation/Editor/`. La guía especializada [Distribution](../Distribution.md) explica el procedimiento de build, la ubicación del ZIP y las comprobaciones de la entrega. No incluir `Library/`, carpetas de builds locales ni perfiles reales del jugador como material fuente.

## Android beta

Requiere Android **7.1 / API 25 o posterior** y **ARM64**. Aunque el APK no es una compilación Development, conserva una **firma Debug**, distinta de una futura firma de publicación. Desinstalar puede borrar los datos locales; conviene respaldar antes de cambiar firma o reinstalar. Revisar [AndroidBuild](../AndroidBuild.md) y [MobilePort](../MobilePort.md) para limitaciones y validación física.

## Progreso local

En Windows, el perfil se conserva bajo `%USERPROFILE%/AppData/LocalLow/Yeco Works/Squid Ink-Pulse/db`. Actualizar la carpeta portátil del ejecutable no restablece ese guardado. Los datos del servidor de feria son otro almacén y no se deben mezclar.
