# Manual del jugador

[← Diseño y experiencia](README.md)

## Empieza aquí

Controlamos a **Baby Squid**, un pequeño calamar en un mundo submarino. El escenario avanza y aparecen peligros: hay que esquivarlos, recoger camarones y decidir cuándo acercarnos lo suficiente para cargar **Ink-Pulse**. La habilidad se puede usar cuando está lista; no es necesario activarla en cuanto se carga.

El menú principal incluye **Cómo jugar**, presentado mediante páginas de cómic. No exige entrar a una escena tutorial adicional.

## Instalar el juego

- **Windows:** descargar [Windows 1.0.1](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/windows-v1.0.1), extraer todo el ZIP y abrir `JUGAR.cmd` o el ejecutable incluido. Mantener juntos los archivos.
- **Android:** descargar [Android 1.0.1 beta 1](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/android-v1.0.1-beta.1), instalar el APK ARM64 en Android 7.1/API 25 o superior. Sigue siendo una versión beta, por lo que controles y rendimiento deben evaluarse en cada dispositivo.

No descargues el ZIP que GitHub genera automáticamente con la etiqueta «Source code» para intentar jugar: contiene código y assets del proyecto, no un player instalado.

## Controles Windows

| Acción | Tecla / dispositivo |
| --- | --- |
| Mover verticalmente a Baby Squid | Mouse |
| Activar Ink-Pulse | Clic o Espacio |
| Pausa | P / Escape |
| Gadget del primer / segundo espacio | Q / W |
| Comprar oferta de tienda durante la partida | B |
| Navegar menús y cómics | Mouse |

En Android se utiliza una interfaz táctil específica; su adaptación y comprobaciones siguen en desarrollo. No extrapolamos botones o gestos exactos sin validar la distribución móvil.

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
- **No puedo jugar desde «Source code»:** necesitas el adjunto ZIP Windows o APK Android de una release.
- **No hay servidor de feria:** no es obligatorio para la experiencia principal.
- **Falla la tienda o se pierde una compra:** evita sobrescribir archivos de guardado sin respaldo y registra la versión y los pasos para reproducir el error.

[Requisitos técnicos y limitaciones](../02-tecnica/compilacion-y-distribucion.md).
