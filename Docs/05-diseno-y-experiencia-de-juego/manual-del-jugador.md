# Manual del jugador

[← Diseño y experiencia](README.md)

## Empieza aquí

Controlamos a **Baby Squid**, un pequeño calamar en un mundo submarino. El escenario avanza y aparecen peligros: hay que esquivarlos, recoger camarones y decidir cuándo acercarnos lo suficiente para cargar **Ink-Pulse**. La habilidad se puede usar cuando está lista; no es necesario activarla en cuanto se carga.

El menú principal incluye **Cómo jugar**, presentado mediante páginas de cómic.

## Instalar el juego

- **Windows:** descargar [Windows 1.0.1](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/windows-v1.0.1), extraer todo el ZIP y abrir `JUGAR.cmd` o el ejecutable incluido. Mantener juntos los archivos.
- **Android:** descargar [Android 1.0.1 beta 1](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/android-v1.0.1-beta.1), instalar el APK ARM64 en Android 7.1/API 25 o superior. Sigue siendo una versión beta, por lo que controles y rendimiento deben evaluarse en cada dispositivo.

Los adjuntos ZIP Windows y APK Android son las descargas para jugar. «Source code» contiene el proyecto de Unity.

## Controles Windows

| Acción | Tecla / dispositivo |
| --- | --- |
| Mover verticalmente a Baby Squid | Mouse |
| Activar Ink-Pulse | Clic o Espacio |
| Pausa | P / Escape |
| Gadget del primer / segundo espacio | Q / W |
| Comprar oferta de tienda durante la partida | B |
| Navegar menús y cómics | Mouse |

## Controles Android

| Acción | Control táctil |
| --- | --- |
| Dirigir a Baby Squid | Mantener y mover un dedo en el área de juego; al soltar, el avance continúa y se conserva la altura. |
| Activar Ink-Pulse | Botón Ink-Pulse cuando la barra indique LISTO. |
| Pausar | Botón de pausa; Reanudar vuelve a la partida. |
| Usar un gadget | Botón de su espacio en el HUD. |
| Comprar | Botón Comprar de la oferta visible. |
| Menús y cómics | Tocar sus botones. |

En el código actual, Atrás cierra opciones y tutorial, vuelve desde la tienda permanente y abre la pausa durante la partida. Al regresar de otra aplicación o de bloquear la pantalla, la partida requiere Reanudar. Estas mejoras posteriores a la beta publicada se incluyen en la próxima build; consulta [navegación e interrupciones](../AndroidLifecycle.md).

## Cómo se juega

1. **Inicia una run** desde el menú.
2. **Evita las colisiones**. Acercarte lo suficiente a ciertas amenazas, sin tocarlas, puede cargar Ink-Pulse.
3. **Observa la barra de tinta** y decide cuándo activar la habilidad para aprovechar la ventaja temporal.
4. **Recoge camarones** para disponer de recursos y comprar cuando aparezca la oportunidad.
5. **Usa gadgets** y descubre las mejoras/skins desde la tienda permanente.
6. **Sobrevive a jefes y portales**, visita las zonas disponibles y busca superar tu récord.
7. Si la partida termina, **reintenta**. Parte del progreso queda guardado localmente.

## Guardado y precauciones

El progreso es **local por dispositivo**. En Windows se guarda bajo `%USERPROFILE%/AppData/LocalLow/Yeco Works/Squid Ink-Pulse/db`. Reemplazar el ZIP portátil no borra automáticamente esos archivos. Antes de reinstalar Android, cambiar firma o limpiar el perfil, haz respaldo de los datos que quieras conservar; desinstalar puede eliminarlos.

## Dificultades frecuentes

- **No abre el ZIP:** primero extrae el contenido completo; no muevas solo el .exe.
- **No hay servidor de feria:** no es obligatorio para la experiencia principal.
- **Falla la tienda o se pierde una compra:** evita sobrescribir archivos de guardado sin respaldo y registra la versión y los pasos para reproducir el error.

[Requisitos técnicos y limitaciones](../02-tecnica/compilacion-y-distribucion.md).
