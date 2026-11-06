<div align="center">

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="branding/banners/squid-banner-oscuro.png">
  <img src="branding/banners/squid-banner-claro.png" alt="Squid Ink-Pulse — videojuego submarino de Yeco Works" width="100%">
</picture>

# Squid Ink-Pulse

**Acércate al peligro. Carga Ink-Pulse. Sobrevive una partida más.**

[![Unity 6](https://img.shields.io/badge/Unity-6000.3.11f1-222831?logo=unity&logoColor=white)](ProjectSettings/ProjectVersion.txt)
[![C%23](https://img.shields.io/badge/Lenguaje-C%23-673AB7)](Assets/Implementation/Code)
[![Windows](https://img.shields.io/badge/Windows-1.0.1-2979B9)](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/windows-v1.0.1)
[![Android](https://img.shields.io/badge/Android-1.0.1%20beta%201-387f60)](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/android-v1.0.1-beta.1)

[**Descargar**](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases) · [**Cómo jugar**](Docs/05-diseno-y-experiencia-de-juego/manual-del-jugador.md) · [**Documentación**](Docs/README.md) · [**Créditos y derechos**](LICENSING.md)

</div>

## Resumen ejecutivo

Squid Ink-Pulse es un *endless runner* submarino 2D creado por **Yeco Works** en la Universidad Católica del Norte. Controlas a Baby Squid: pasar cerca del peligro sin chocar carga **Ink-Pulse**, una habilidad que convierte el riesgo en una ventaja temporal. El juego reúne dos zonas, enemigos, jefes, gadgets y mejoras con progreso local; está disponible para **Windows** y como **beta para Android**.

## Descarga e instalación

| Plataforma | Entrega publicada | Cómo iniciar |
| --- | --- | --- |
| **Windows x64** | [v1.0.1, estable](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/windows-v1.0.1) | Extrae el ZIP completo y abre `JUGAR.cmd`. |
| **Android ARM64** | [v1.0.1 beta 1](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/android-v1.0.1-beta.1) | Instala el APK en Android 7.1/API 25 o posterior. |

Descarga el ZIP Windows o el APK adjunto a la release. Los archivos «Source code» contienen el proyecto de Unity. Las notas de cada versión incluyen requisitos y checksums; Android mantiene firma de desarrollo y continúa en beta.

El progreso se guarda en el dispositivo. En Windows, actualizar la carpeta del juego conserva los datos de `%USERPROFILE%/AppData/LocalLow/Yeco Works/Squid Ink-Pulse/db`. En Android, desinstalar puede borrar el progreso.

## Cómo jugar

Acércate a las amenazas para cargar la barra de tinta, activa Ink-Pulse cuando lo necesites y recoge camarones para las tiendas. Los portales conectan las zonas epipelágica y abisopelágica. El menú incluye un cómic de **Cómo jugar**.

| Acción | Windows | Android |
| --- | --- | --- |
| Dirigir al calamar | Mouse | Mantener y mover un dedo sobre el área de juego |
| Activar Ink-Pulse | Clic o Espacio | Botón Ink-Pulse |
| Pausa | P o Escape | Botón de pausa |
| Usar gadgets | Q / W | Botón del espacio correspondiente |
| Comprar durante la partida | B o botón Comprar | Botón Comprar |

[Manual del jugador](Docs/05-diseno-y-experiencia-de-juego/manual-del-jugador.md)

## Abrir el proyecto

1. Clona el repositorio y añade su carpeta a Unity Hub.
2. Abre con **Unity 6000.3.11f1** y espera a que se importen los paquetes.
3. Abre `Assets/Scenes/MainMenu/MainMenu.unity` y pulsa **Play**.

| Tecnología | Uso |
| --- | --- |
| Unity y C# | Escenas, lógica y compilación del juego. |
| Universal Render Pipeline 2D | Renderizado, luces y efectos. |
| Unity Input System | Entrada de mouse, teclado y controles táctiles. |
| JSON local | Perfil, catálogo, récords y clasificación del dispositivo. |
| Python y SQLite | Complemento opcional de feria para Windows, con ranking web en red local. |

Para generar un ejecutable o APK, consulta [compilación y distribución](Docs/02-tecnica/compilacion-y-distribucion.md).

## Documentación

| Área | Contenido |
| --- | --- |
| [General](Docs/01-general/README.md) | Origen, propuesta, alcance y equipo. |
| [Técnica](Docs/02-tecnica/README.md) | Arquitectura, datos, estados, plataformas y feria. |
| [Desarrollo](Docs/03-desarrollo/README.md) | Metodología, estructura, pruebas y próximos pasos. |
| [Académica y formativa](Docs/04-academica/README.md) | Contexto del curso e informes originales. |
| [Diseño y experiencia de juego](Docs/05-diseno-y-experiencia-de-juego/README.md) | GDD, controles, zonas, narrativa y presentación. |

## Equipo y condiciones de uso

Yeco Works estuvo integrado por **Pablo Guzmán, Rodrigo Cortés, Mauricio Muñoz, Inti Santibáñez y Matías Palacios**. Desarrollamos el juego en **Proyecto Integrador Programación Avanzada**, quinto semestre, 5 SCT, de la Universidad Católica del Norte.

Los roles, las contribuciones artísticas y los derechos se detallan en [LICENSING.md](LICENSING.md). La licencia general de reutilización sigue pendiente de acuerdo entre sus titulares.
