# Auditoria de jerarquia runtime

## Proposito

Este documento define que script debe vivir en cada nodo principal de escena o prefab. Su funcion es evitar contradicciones: dos scripts intentando gobernar el mismo movimiento, tags configurables que compiten con el catalogo central, o componentes heredados que siguen serializados aunque ya no tengan responsabilidad.

## Regla central

Un nodo debe tener un solo propietario por responsabilidad.

- Movimiento de camara: `CameraController`.
- Movimiento horizontal del mundo y boundaries: `HorizontalTracker`.
- Spawn de enemigos: `LevelSpawner` mediante `enemyProfiles`.
- Ritmo macro de spawn: `RunProgressionDirector`.
- Reconocimiento de enemigos: `EnemyTagCatalog`.
- Colision fisica del jugador: `PlayerCollision`.
- Carga por proximidad: `GrazeDetector`.
- Estado runtime del jugador: `PlayerStateController`.
- Ink-Pulse: `InkPulseController`.
- Evento SS Carnage: `BossEventDirector`, `SSCarnageController` y `SSCarnageNetWall`.
- UI de pausa: `PauseMenuManager`.
- UI de game over: `GameOverMenuManager`.
- Animacion de botones de menu: `MenuButtonAnimation`.

## Jerarquia de ZonaEpipelagica

| Nodo | Script esperado | Responsabilidad |
| --- | --- | --- |
| `GameSession` | `GameSessionController`, `RunProgressionDirector` | Estado de partida y progresion temporal. |
| `SceneFlow` | `SceneFlowController` | Carga de escenas y retorno al menu. |
| `LevelSpawner` | `LevelSpawner` | Spawn de monedas y enemigos perfilados. |
| `Main Camera` | `CameraController` | Seguimiento y eventos de camara. |
| `Boundaries` | `HorizontalTracker` | Mantener boundaries alineados con el avance horizontal. |
| `Squid` | `PlayerMovement`, `InkPulseController`, `ShrimpCollector`, `PlayerCollision`, `PlayerStateController` | Control completo del jugador. |
| `GrazeZone` | `GrazeDetector` | Carga de Ink-Pulse por proximidad a amenazas. |
| `GarbageCollector` | `DestroyOffscreen` | Destruir enemigos y camarones que salen del area util. |
| `SSCarnageManager` | `BossEventDirector` | Disparar y coordinar eventos de boss. |
| `PauseMenuManager` | `PauseMenuManager` | Abrir, cerrar y cablear el menu de pausa. |
| `GameOverMenuManager` | `GameOverMenuManager` | Abrir, cerrar y cablear el panel de derrota. |
| Botones de pausa/game over | `MenuButtonAnimation` | Animacion interactiva visual del boton. |
| Fondo burbujas UI | `MenuBubbles` | Movimiento decorativo compartido de burbujas. |
| `InkPulseBar` | `ChargeBar` | Representacion visual de carga Ink-Pulse. |

## Prefabs runtime

| Prefab | Script esperado | Tag esperado |
| --- | --- | --- |
| `PezGlobo` | `PufferfishEnemy` | `EnemyPezGlobo` |
| `Mina` | ninguno por ahora | `EnemyMina` |
| `CanaPescar` | ninguno por ahora | `EnemyCanaPescar` |
| `ShrimpCoin` | `ShrimpValue` | `Shrimp` |
| `ShrimpCoinX10` | `ShrimpValue` | `Shrimp` |
| `SSCarnage` | `SSCarnageController` | `SSCarnage` |
| `BossNetWall` | `SSCarnageNetWall` | `SSCarnage` |

La mina y la cana no tienen script propio todavia porque su logica actual vive en el algoritmo de spawn. Cuando reciban comportamiento autonomo, deben incorporarse scripts dedicados en `Assets/Implementation/Code/Enemies/`.

## Spawn de enemigos

`LevelSpawner` ya no usa un `enemyPrefab` generico heredado. Todo enemigo debe nacer desde `enemyProfiles`, donde cada entrada define:

- `prefab`: prefab concreto a instanciar.
- `enemyTag`: tag logico del enemigo.
- `baseWeight`: peso relativo de aparicion.
- `minIntensity`: intensidad minima de run.
- `spawnIntervalMultiplier`: modificador local del intervalo tras ese spawn.

Despues de instanciar, `LevelSpawner` aplica el tag con `EnemyTagCatalog.ApplyEnemyTag()` y asigna la capa `Enemy` de forma recursiva.

## Tags

Los tags de enemigos tienen una sola fuente de verdad:

- `EnemyTagCatalog.Generic`
- `EnemyTagCatalog.Mine`
- `EnemyTagCatalog.Pufferfish`
- `EnemyTagCatalog.FishingRod`

`PlayerCollision`, `GrazeDetector` y `DestroyOffscreen` no deben exponer campos editables para tags de enemigos. Esto evita que el Inspector contradiga al catalogo central.

## Scripts retirados o reemplazados

| Script anterior | Estado | Motivo |
| --- | --- | --- |
| `CameraFollowHorizontal` | eliminado | Duplicaba responsabilidades de `CameraController` y `HorizontalTracker`. |
| `PauseButtonAnimation` | reemplazado por `MenuButtonAnimation` | Su uso ya no es exclusivo del menu de pausa. |
| `PauseBubbles` | eliminado | `MenuBubbles` cubre la animacion decorativa compartida. |

## Invariantes de mantenimiento

- No agregar un segundo script de movimiento a `Main Camera`.
- No agregar un fallback generico de enemigo al spawner si ya existen perfiles.
- No bloquear el spawn regular durante `BossActive`; el evento debe duplicar frecuencia, no detener obstaculos.
- No serializar tags de enemigos en `PlayerCollision` o `GrazeDetector`.
- No usar scripts de pausa en nodos de game over.
- No poner logica de boss en el prefab de red que ya pertenece a `SSCarnageController`.
- Si un nuevo enemigo necesita comportamiento, debe tener prefab, tag, perfil de spawn y script propio documentados juntos.
