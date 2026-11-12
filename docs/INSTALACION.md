# Instalación y funcionamiento

[← Inicio](../README.md) · [Releases](../../../releases) · [Créditos](../CREDITS.md)

## Windows 64 bits

1. En [Releases](../../../releases), selecciona **Squid Ink-Pulse — Windows v1.0.1**.
2. Descarga **`SquidInkPulse-Windows-x64-v1.0.1.zip`**.
3. Extrae **todo el ZIP** a una carpeta escribible.
4. Ejecuta **`JUGAR.cmd`**. Conserva los archivos que acompañan al ejecutable.
5. El perfil se guarda por defecto en `%USERPROFILE%/AppData/LocalLow/Yeco Works/Squid Ink-Pulse/db`; no es la misma carpeta que los binarios.

## Android ARM64 (beta)

1. En [Releases](../../../releases), selecciona la beta de Android v1.0.1.
2. Descarga **`SquidInkPulse-Android-arm64-v1.0.1-beta.1.apk`**.
3. Confirma la procedencia y, solo si decides instalar, habilita la instalación desde esa fuente durante el proceso.
4. La versión es una **beta**, no una publicación de Google Play. La compatibilidad varía según dispositivo y versión de Android.
5. **La desinstalación podría borrar el progreso**. La actualización de una instalación anterior depende de la compatibilidad entre las firmas del APK.

## Controles

| Acción | Windows | Android |
| --- | --- | --- |
| Mover a Baby Squid | Ratón | Mantener/mover un dedo en el área de juego |
| Ink-Pulse | Clic o Espacio | Botón Ink-Pulse |
| Pausa | P o Escape | Botón de pausa |
| Gadgets | Q / W | Botones de gadget |
| Tienda durante partida | B o botón Comprar | Botón Comprar |

## Integridad de descargas

Cada release de distribución incorpora un archivo `.sha256` junto al binario. Comprueba la suma SHA-256 del ZIP/APK si necesitas verificar que el archivo descargado coincida con el publicado. En Windows puedes usar PowerShell: `Get-FileHash -Algorithm SHA256 .\archivo.zip`.

## Privacidad y almacenamiento

La progresión principal del jugador se conserva localmente en su dispositivo. No se requieren cuentas remotas para la partida normal. La herramienta de clasificaciones de eventos de feria no se incluye en esta distribución pública.

## Código fuente

Este repositorio **no es un paquete del proyecto Unity**. El enlace automático de GitHub *Source code* descarga los textos públicos de este repositorio, no los scripts ni escenas del juego. La descarga que debes utilizar para jugar es el **ZIP de Windows o la APK Android** adjunta a una release.
