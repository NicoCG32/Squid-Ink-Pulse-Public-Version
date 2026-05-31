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
| Boss específico | `SSCarnageAttackState` | ¿En qué fase interna está el ataque del SS Carnage? |
| Cámara | `CameraEventMode` | ¿Seguir, vista amplia para evento o volver a seguir? |

## Estados implementados

- `GameSessionState`
- `PlayerRuntimeState`
- `RunEventState`
- `InkPulseState`
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

## Estados planificados

- `SupplyEventState`
- `PortalTransitionState`
- `GadgetRuntimeState`

## Regla base

- Si un sistema cambia comportamiento, habilita una interacción o evita ambigüedad entre otros sistemas, merece estado propio.
- Si el sistema futuro puede resolverse con un `bool` sin perder claridad, no necesita otra máquina.
