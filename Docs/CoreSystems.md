# Sistemas núcleo

## Alcance

Este documento cubre la capa de orquestación del juego: sesión global, progresión de run, flujo de escenas y reglas base que afectan a todos los sistemas.

## GameSessionController

Archivo: `Assets/Implementation/Code/Core/Session/GameSessionController.cs`

Responsabilidad:
- Controlar el estado global del juego.
- Aplicar `Time.timeScale` según el estado.
- Exponer eventos para que otros sistemas respondan sin acoplarse directamente.

Estados:
- `Playing`
- `Paused`
- `GameOver`

## RunProgressionDirector

Archivo: `Assets/Implementation/Code/Core/Session/RunProgressionDirector.cs`

Responsabilidad:
- Llevar el ritmo de la run.
- Calcular dificultad, intensidad, scroll y spawn.
- Gestionar ventanas de boss y transición.
- Modular la frecuencia de spawn según el estado macro de la run.

Estados de evento:
- `Normal`
- `BossActive`
- `PostBossWindow`
- `Transitioning`

Reglas de spawn por evento:
- `Normal`: usa el intervalo base calculado por intensidad.
- `BossActive`: reduce el intervalo con `bossActiveSpawnIntervalMultiplier`; por defecto `0.5`, equivalente a doble frecuencia.
- `PostBossWindow`: aumenta el intervalo con `postBossSpawnIntervalMultiplier`; por defecto `1.75`, equivalente a una ventana de reposo con menor presión.
- `Transitioning`: bloquea spawn regular.

## SceneFlowController

Archivo: `Assets/Implementation/Code/Core/Scenes/SceneFlowController.cs`

Responsabilidad:
- Cargar escenas por nombre o índice.
- Reiniciar la escena actual.
- Volver al menú principal.

## ReglasCompartidas

- La sesión global manda sobre el resto de subsistemas.
- La progresión no debe mezclarse con la lógica de UI.
- Los cambios de escena deben restaurar `Time.timeScale` a `1`.
- La progresión de run debe ser consultable desde cualquier sistema de spawn o boss.
