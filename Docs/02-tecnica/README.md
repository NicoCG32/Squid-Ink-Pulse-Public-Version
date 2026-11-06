# 02 · Documentación técnica

[← Índice](../README.md)

| Documento | Contenido |
| --- | --- |
| [Arquitectura](arquitectura.md) | Ejecución, componentes, dependencias y estructura. |
| [Dominio y persistencia](dominio-y-persistencia.md) | Estado de partida, perfil, JSON local y SQLite de feria. |
| [Estados y flujos](estados-y-flujos.md) | Navegación, pausa, Ink-Pulse, portales y derrota. |
| [Integración de feria](integracion-feria.md) | Servidor del evento y ranking web en la LAN. |
| [Compilación y distribución](compilacion-y-distribucion.md) | Abrir el proyecto, generar builds e instalar las descargas. |

## Código por responsabilidad

- `Assets/Implementation/Code/Core/`: sesión, escenas, progresión y ciclo de vida.
- `Assets/Implementation/Code/Player/`: movimiento, estado, habilidades y perfil.
- `Assets/Implementation/Code/Spawning/`: selección y aparición de amenazas.
- `Assets/Implementation/Code/Bosses/` y `Enemies/`: jefes y comportamientos.
- `Assets/Implementation/Code/UI/` y `Input/`: presentación e interacción.
- `Tools/FairServer/`: servidor opcional de feria.

Los contratos detallados están en [arquitectura de software](../SoftwareArchitecture.md), [jerarquía de escenas y prefabs](../SceneHierarchy.md) y [estructura del proyecto](../ProjectStructure.md).
