# Estados y flujos

[← Área técnica](README.md)

La sesión, el jugador, Ink-Pulse y los eventos de partida tienen estados propios. Cada controlador aplica las transiciones de su sistema.

## Flujo del jugador y escenas

```mermaid
flowchart TD
  Start(["Abrir juego"]) --> Menu["Menú principal"]
  Menu --> Help["Cómic Cómo jugar"]
  Help --> Menu
  Menu --> Shop["Tienda permanente"]
  Shop --> Menu
  Menu --> Run["Zona epipelágica / partida"]
  Run --> Pulse["Graze y uso de Ink-Pulse"]
  Pulse --> Run
  Run --> Offer["Tienda temporal / gadgets"]
  Offer --> Run
  Run --> Portal{"¿Cruza un portal?"}
  Portal -->|Sí| Other["Zona abisopelágica"]
  Other --> Run
  Portal -->|No| Run
  Run --> Death["Game Over"]
  Death --> Retry{"¿Reintenta?"}
  Retry -->|Sí| Run
  Retry -->|No| Menu
```

Las tiendas, jefes y portales aparecen según la progresión y los perfiles de cada zona.

## Sesión global

Estados de `GameSessionState.cs`: `Playing`, `Paused`, `GameOver`.

```mermaid
stateDiagram-v2
  [*] --> Playing
  Playing --> Paused: solicitar pausa
  Paused --> Playing: reanudar
  Playing --> GameOver: derrota
  GameOver --> [*]: salir / cargar nueva escena
```

`GameSessionController` aplica `Time.timeScale`; al entrar en GameOver registra el resultado y limpia estado temporal de la partida.

## Macroeventos de partida

Estados en `RunEventState.cs`:

```mermaid
stateDiagram-v2
  [*] --> Normal
  Normal --> BossActive: inicia evento de boss
  BossActive --> PostBossWindow: boss resuelto
  PostBossWindow --> Transitioning: entrar en portal
  Transitioning --> Normal: cargar zona destino
  PostBossWindow --> Normal: continuar sin portal
```

`RunProgressionDirector` controla las condiciones y los tiempos de estos eventos.

## Jugador e Ink-Pulse

```mermaid
stateDiagram-v2
  [*] --> Moving
  Moving --> InkPulse: habilidad activada
  InkPulse --> Moving: finaliza efecto
  Moving --> PortalTransition: entra al portal
  InkPulse --> PortalTransition: transición de zona
  Moving --> Death: daño letal
  InkPulse --> Death: derrota
  PortalTransition --> [*]: carga de escena
```

```mermaid
stateDiagram-v2
  [*] --> Idle
  Idle --> Charging: empieza carga
  Charging --> Ready: umbral alcanzado
  Ready --> Active: activación permitida
  Active --> Idle: efecto termina
```

Los estados formales están en `PlayerRuntimeState.cs` e `InkPulseState.cs`. El graze carga Ink-Pulse al pasar cerca de una amenaza sin chocar. El controlador de la habilidad restringe su activación durante tiendas, portales y otros estados bloqueantes.

## Estados adicionales

| Subsistema | Estados formales |
| --- | --- |
| Tienda temporal | `Closed`, `Offering` |
| Cámara | `Follow`, `WideEvent`, `ReturningToFollow` |
| SS Carnage | `Inactive`, `Warning`, `DeployingNet`, `NetActive`, `Resolved`, `Failed`, `Exiting`, `Finished` |

Los efectos y las condiciones de transición se detallan en [máquinas de estado](../StateMachines.md).
