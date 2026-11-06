# Estados y flujos

[← Área técnica](README.md)

El juego tiene varias máquinas de estado, y cada una responde a una pregunta distinta. No conviene mezclarlas en una sola máquina gigantesca.

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

El diagrama es una **vista de navegación**; no significa que todas las tiendas o eventos estén disponibles en cualquier momento ni especifica el orden exacto de los bosses.

## Sesión global

Estados comprobados en `GameSessionState.cs`: `Playing`, `Paused`, `GameOver`.

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

Esta es una **secuencia conceptual de eventos**; revisar condiciones y tiempos exactos en `RunProgressionDirector`.

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

Los estados formales están en `PlayerRuntimeState.cs` e `InkPulseState.cs`. El graze proporciona carga, pero **graze no equivale a colisión dañina**. El control de la habilidad aplica sus propias restricciones durante tienda, portal y otros estados.

## Estados adicionales

| Subsistema | Estados formales |
| --- | --- |
| Tienda temporal | `Closed`, `Offering` |
| Cámara | `Follow`, `WideEvent`, `ReturningToFollow` |
| SS Carnage | `Inactive`, `Warning`, `DeployingNet`, `NetActive`, `Resolved`, `Failed`, `Exiting`, `Finished` |

Los enum indican las fases **posibles**, no prueban por sí solos todas sus transiciones. Para detalles y guardas de cada sistema, consultar [StateMachines](../StateMachines.md).
