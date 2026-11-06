# Compilación y distribución

[← Área técnica](README.md)

## Descargas

| Plataforma | Versión publicada | Instalación |
| --- | --- | --- |
| Windows x64 | [v1.0.1, estable](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/windows-v1.0.1) | Extraer el ZIP completo y abrir `JUGAR.cmd`. |
| Android ARM64 | [v1.0.1 beta 1](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/android-v1.0.1-beta.1) | Instalar el APK en Android 7.1/API 25 o posterior. |

Los adjuntos `SHA256SUMS.txt`, `source-info.json` y `SOURCE.patch` identifican la integridad y procedencia de cada entrega. «Source code» descarga el proyecto de Unity. Las mejoras posteriores del repositorio llegan al juego instalado cuando se compila y publica una nueva versión.

## Abrir el proyecto

1. Instalar Unity Hub y **Unity 6000.3.11f1**, indicado en `ProjectSettings/ProjectVersion.txt`.
2. Añadir el módulo Windows Build Support o Android Build Support según el destino. Para Android, usar también SDK, NDK y OpenJDK de Unity Hub.
3. Añadir el repositorio como proyecto existente y esperar la importación de paquetes.
4. Abrir `Assets/Scenes/MainMenu/MainMenu.unity` y ejecutar Play Mode.

Las escenas habilitadas son `MainMenu`, `ZonaEpipelagica`, `ZonaAbisopelagica` y `ShopMenu`. El cómic de «Cómo jugar» está disponible en el menú; `ZonaTutorial` queda fuera de compilación.

## Generar builds

- **Windows:** `WindowsReleaseBuilder.BuildWindowsRelease` crea el player sin Development. [Distribución](../Distribution.md) explica la compilación y el empaquetado.
- **Android:** `AndroidDevelopmentBuilder` ofrece APK Development y APK sin Development. [Build Android](../AndroidBuild.md) incluye comandos, instalación, firma y revisión de permisos.

Antes de distribuir, ejecutar las [pruebas y recorridos de validación](../03-desarrollo/pruebas-y-validacion.md). Mantener los binarios en los adjuntos de Releases y conservar sus notas y checksums.

## Datos al actualizar

En Windows, el perfil se guarda en `%USERPROFILE%/AppData/LocalLow/Yeco Works/Squid Ink-Pulse/db`. Cambiar la carpeta del ejecutable conserva esos archivos.

La beta Android mantiene firma de desarrollo. Actualizar requiere un certificado compatible; desinstalar puede borrar el progreso. La futura candidata necesita firma de distribución y completar el [recorrido móvil](../MobilePort.md). Los datos del servidor de feria se respaldan por separado.
