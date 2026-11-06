# Estructura y criterios para continuar el desarrollo

[← Desarrollo](README.md)

## Dónde hacer los cambios

| Necesidad | Carpeta o referencia |
| --- | --- |
| Mecánica y estados de jugador | `Assets/Implementation/Code/Player/` |
| Sesión y transición de escenas | `Assets/Implementation/Code/Core/` |
| Enemigos y jefes | `Assets/Implementation/Code/Enemies/`, `Bosses/` |
| Aparición y dificultad | `Assets/Implementation/Code/Spawning/` |
| Menús, HUD y tiendas | `Assets/Implementation/Code/UI/` |
| Controles e interfaz táctil | `Assets/Implementation/Code/Input/` y `UI/Touch/` |
| Persistencia, precios, desbloqueos | `Assets/Implementation/Code/Player/Profile/` |
| Prefabs, escenas, arte y sonido | `Assets/Content/`, `Assets/Scenes/` |
| Editor, validación y builds | `Assets/Implementation/Editor/` |
| Tests | `Assets/Tests/EditMode/` y `Assets/Tests/PlayMode/` |

## Antes de modificar una mecánica

Revisar el controlador propietario, su estado formal, su configuración serializada y la escena que lo instancia. Un script correcto puede no ser funcional si la referencia en Inspector o el prefab está rota. Identificar también quién actualiza el HUD y cómo se limpia el estado al finalizar la partida.

Para datos permanentes, revisar migraciones y normalizaciones antes de agregar campos. Las partidas existentes no deben perder su progreso por una actualización del catálogo.

## Convenciones que protege este repositorio

- El **prefab fuente** de BabySquid concentra las partes reutilizables del personaje.
- Las escenas asignan sus referencias externas y los perfiles controlan la aparición de amenazas.
- Los **enum de estado** describen fases; los controladores aplican las transiciones.
- Los **JSON de Resources** son datos iniciales; los guardados del dispositivo viven en `persistentDataPath`.
- El **servidor de feria** es opcional, no una condición de compilación o juego.
- Las mejoras para Android deben comprobarse en dispositivo físico; el editor y el simulador no demuestran ergonomía real.

## Actualizar la documentación

Si una característica cambia, actualizar primero el capítulo de su responsabilidad: guía de juego si cambia la regla visible; referencia técnica si cambia contrato o estructura; guía de desarrollo si cambia el procedimiento. Los **informes académicos originales nunca se reescriben** para reflejar una actualización posterior.

Referencias más detalladas: [SoftwareArchitecture](../SoftwareArchitecture.md), [Input](../Input.md), [PersistentProfile](../PersistentProfile.md) y [ProjectStructure](../ProjectStructure.md).
