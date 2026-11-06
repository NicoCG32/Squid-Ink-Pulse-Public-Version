# Arquitectura del videojuego

[← Área técnica](README.md)

Unity ejecuta el juego, su interfaz y el guardado local. Windows puede conectarse además a un servidor de feria en red local; Android mantiene la experiencia sin conexión.

## Contexto y ejecución

```text
[Jugador]
    |
    v
[Juego Unity: Windows / Android] <--> [Perfil y récords JSON del dispositivo]
    |
    | Windows, modo feria opcional
    v
[Servidor HTTP Python en el host] <--> [SQLite del evento]
    ^
    | clasificación web
[Navegadores de la misma LAN]
```

El complemento de feria almacena y muestra los resultados del PC anfitrión. Su base de datos es independiente del perfil local del juego. La operación del evento se explica en [integración de feria](integracion-feria.md).

## Componentes de Unity

```text
[Mouse / teclado / UI táctil]
             |
             v
[Lector de comandos] --> [Movimiento / Ink-Pulse / gadgets]
                                      |
                              [Colisión y graze]
                                      |
                               [HUD / audio / visuales]

[GameSessionController] <--> [RunProgressionDirector]
             |                         |
             v                         v
[SceneFlowController]          [LevelSpawner + perfiles]
             |                         |
       [Zonas / portales]        [Enemigos / jefes / tiendas]

[Tiendas / resultados] --> [Servicios de perfil] --> [JSON local]
```

El esquema agrupa responsabilidades. Los controladores aplican reglas; los estados describen fases; los perfiles y catálogos contienen configuración.

| Componente | Responsabilidad |
| --- | --- |
| `GameSessionController` | Partida activa, pausa y Game Over. |
| `RunProgressionDirector` | Score, ritmo y eventos de la partida. |
| `SceneFlowController` | Navegación y cambio de zona. |
| `SquidInkPulseGameplayInputReader` | Comandos semánticos de entrada. |
| `PlayerMovement`, `InkPulseController`, `PlayerGadgetInventory` | Movimiento, habilidad e inventario temporal. |
| `LevelSpawner` | Aparición de entidades a partir de perfiles de zona. |
| `PersistentPlayerProfile`, `PlayerProfileRepository` | Perfil, economía y persistencia local. |
| `MobileBackNavigation`, `MobileApplicationLifecycle` | Atrás e interrupciones de Android. |

## Estructura y compilación

| Ruta | Función |
| --- | --- |
| `Assets/Scenes/` | Escenas serializadas. |
| `Assets/Implementation/Code/` | Lógica y adaptadores Unity en C#. |
| `Assets/Implementation/Config/` | Perfiles de configuración e Input System. |
| `Assets/Content/` | Prefabs, arte, animaciones y audio. |
| `Assets/Implementation/Resources/PersistentDbSeeds/` | Semillas JSON de nuevas instalaciones. |
| `Packages/` y `ProjectSettings/` | Dependencias y configuración del proyecto. |
| `Tools/FairServer/` | Servidor Python/SQLite de feria. |

```text
[Fuentes + paquetes] --> [Unity 6000.3.11f1] --> [Player Windows x64] --> [ZIP]
                                           \-> [Player Android ARM64] --> [APK]
```

Las escenas habilitadas son `MainMenu`, `ZonaEpipelagica`, `ZonaAbisopelagica` y `ShopMenu`. `ZonaTutorial` permanece fuera del build; el tutorial activo es el cómic del menú.

Detalles: [arquitectura de software](../SoftwareArchitecture.md), [jerarquía de escenas y prefabs](../SceneHierarchy.md), [sistemas centrales](../CoreSystems.md), [estructura](../ProjectStructure.md) e [Input](../Input.md).
