# Máquinas de estado

## Resumen

Este documento registra las máquinas de estado formales de Squid Ink-Pulse y las máquinas planificadas para los sistemas que todavía no están implementados.

El criterio de formalización es el siguiente: un estado merece existir si cambia comportamiento sistémico, habilita o bloquea interacciones, gobierna una transición importante, o evita que varios sistemas dependan de temporizadores y banderas sueltas.

## Jerarquía conceptual

Las máquinas no compiten entre sí; cada una gobierna una escala distinta.

| Nivel | Máquina | Pregunta que responde |
|---|---|---|
| Global | `GameSessionState` | ¿La simulación está jugando, pausada o terminada? |
| Macro run | `RunEventState` | ¿La run está en flujo normal, boss, post-boss o transición? |
| Entidad jugador | `PlayerRuntimeState` | ¿El jugador se mueve, está en Ink-Pulse o murió? |
| Recurso del jugador | `InkPulseState` | ¿El Ink-Pulse está vacío, cargando, listo o activo? |
| Evento de suministro | `ShopEventState` | ¿La tienda temporal está cerrada u ofreciendo un gadget? |
| Boss específico | `SSCarnageAttackState` | ¿En qué fase interna está el ataque del SS Carnage? |
| Cámara | `CameraEventMode` | ¿Seguir, vista amplia para evento o volver a seguir? |

## Estados implementados

- `GameSessionState`
- `PlayerRuntimeState`
- `RunEventState`
- `InkPulseState`
- `ShopEventState`
- `SSCarnageAttackState`
- `CameraEventMode`

### RunEventState

`RunEventState` gobierna la presión macro de la run y no debe confundirse con el estado interno de un boss.

| Estado | Efecto sobre spawn | Efecto sobre bosses |
|---|---|---|
| `Normal` | Frecuencia base según intensidad. | Puede disparar un nuevo boss si el intervalo se cumple. |
| `BossActive` | Frecuencia duplicada por defecto mediante intervalo `0.5x`. | No puede disparar otro boss. |
| `PostBossWindow` | Frecuencia rebajada por defecto mediante intervalo `1.75x`. | No puede disparar otro boss. |
| `Transitioning` | Bloquea spawn regular. | No puede disparar otro boss. |

### ShopEventState

`ShopEventState` gobierna el overlay temporal de suministros. No sustituye a `GameSessionState`: la partida sigue conceptualmente en `Playing`, pero el manager puede congelar `Time.timeScale` mientras el contador de tienda avanza con tiempo real.

| Estado | Efecto |
|---|---|
| `Closed` | No hay oferta visible ni bloqueo de interacción UI. |
| `Offering` | La tienda muestra un gadget aleatorio, su precio y un contador de expiración. La compra se intenta con `B`. |

## Estados planificados

- `PortalTransitionState`
- `GadgetRuntimeState`

Nota sobre gadgets:
- El inventario de gadgets ya existe como modelo runtime por posesión única y slots.
- La asignacion de slots es temporal y deriva del orden de adquisicion: `Gadget1` usa `Q` si contiene un activo; `Gadget2` usa `W` si contiene un activo.
- `Shell Shield` ocupa slot visual, pero no muestra tecla porque su efecto es pasivo y se consume como salvavidas antes del Game Over.
- `Ink-Bottle` es activo: al usarse, intenta llevar `InkPulseState` directamente a `Ready`.
- `GadgetRuntimeState` sigue planificado para cuando existan efectos con fases propias, cooldowns, duraciones o animaciones de activación.

## Regla base

- Si un sistema cambia comportamiento, habilita una interacción o evita ambigüedad entre otros sistemas, merece estado propio.
- Si el sistema futuro puede resolverse con un `bool` sin perder claridad, no necesita otra máquina.
