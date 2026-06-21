# Maquinas de estado

## Resumen

Este documento registra las maquinas de estado formales de Squid Ink-Pulse y las maquinas planificadas para sistemas que todavia no necesitan una implementacion completa.

Un estado merece existir si cambia comportamiento sistemico, habilita o bloquea interacciones, gobierna una transicion importante, o evita que varios sistemas dependan de temporizadores y banderas sueltas.

La regla arquitectonica completa esta en [SoftwareArchitecture.md](SoftwareArchitecture.md). En particular, un `...State` modela la fase; el `...Controller`, `...Manager` o `...Director` dueno ejecuta la transicion y aplica efectos sobre las especializaciones.

## Jerarquia conceptual

| Nivel | Maquina | Pregunta que responde |
| --- | --- | --- |
| Global | `GameSessionState` | La simulacion esta jugando, pausada o terminada? |
| Macro run | `RunEventState` | La run esta en flujo normal, boss, post-boss o transicion? |
| Entidad jugador | `PlayerRuntimeState` | El jugador se mueve, esta en Ink-Pulse, esta cruzando portal o murio? |
| Recurso del jugador | `InkPulseState` | El Ink-Pulse esta vacio, cargando, listo o activo? |
| Evento de suministro | `ShopEventState` | La tienda temporal esta cerrada u ofreciendo un gadget? |
| Tutorial | `TutorialStep` | Que mecanica pedagogica debe ensenarse o validarse ahora? |
| Boss especifico | `SSCarnageAttackState` | En que fase interna esta el ataque del SS Carnage? |
| Camara | `CameraEventMode` | Seguir, abrir vista amplia o volver al seguimiento? |
| Feedback visual de zona | ciclo no formal de `ZoneLightingController` | La zona esta oscura, revelada o volviendo a oscuridad? |

## Estados implementados

- `GameSessionState` (`Core/Session/GameSessionState.cs`)
- `RunEventState` (`Core/Session/RunEventState.cs`)
- `PlayerRuntimeState` (`Player/State/PlayerRuntimeState.cs`)
- `InkPulseState` (`Player/Abilities/InkPulseState.cs`)
- `ShopEventState` (`UI/Shop/ShopEventState.cs`)
- `TutorialStep` (`Tutorial/TutorialStep.cs`)
- `SSCarnageAttackState` (`Bosses/SSCarnage/SSCarnageAttackState.cs`)
- `CameraEventMode` (`Core/Camera/CameraEventMode.cs`)

### GameSessionState

| Estado | Efecto |
| --- | --- |
| `Playing` | Gameplay activo y `Time.timeScale` normal. |
| `Paused` | Pausa global, menus interactuables. |
| `GameOver` | Derrota; reinicia gadgets, Ink-Pulse, score y pace para la siguiente partida. |

### RunEventState

`RunEventState` gobierna la presion macro de la run y no debe confundirse con el estado interno de un boss.

| Estado | Efecto sobre spawn | Efecto sobre bosses |
| --- | --- | --- |
| `Normal` | Frecuencia base segun intensidad. | Puede disparar un nuevo boss si el intervalo se cumple. |
| `BossActive` | Fuerza intensidad maxima y frecuencia duplicada por defecto mediante intervalo `0.5x`. | No puede disparar otro boss. |
| `PostBossWindow` | Mantiene la run intensa mientras aparece la oportunidad de portal. | Reinicia el reloj interno de boss para evitar otro Carnage inmediato. |
| `Transitioning` | Bloquea spawn regular y pausa acumulacion de score/velocidad; al cruzar portal, la zona destino vuelve a empezar relajada en intensidad de spawn. | No puede disparar otro boss. |

### PlayerRuntimeState

| Estado | Efecto |
| --- | --- |
| `Moving` | Movimiento normal y animacion base. |
| `InkPulse` | Movimiento impulsado durante Ink-Pulse y animacion visual de impulso no-loop. |
| `PortalTransition` | Bloquea movimiento e input nuevo de Ink-Pulse mientras reproduce `PortalEffect` antes de cargar la escena destino. |
| `Death` | Estado de derrota. |

`PlayerStateController` traduce eventos de `InkPulseController` y `ScenePortal` a estado del jugador y comunica el cambio a `PlayerMovement`. No gobierna animadores directamente.

La presentacion visual vive en `PlayerVisualStateController`, ubicado en el root de `BabySquid`. Este controlador observa `PlayerRuntimeState` y aplica prioridad estricta:

| Prioridad | Visual visible |
| --- | --- |
| 1 | `PortalVisual` durante `PlayerRuntimeState.PortalTransition`. |
| 2 | `InkPulseVisual` durante `PlayerRuntimeState.InkPulse`. |
| 3 | `SquidVisual` durante `PlayerRuntimeState.Moving` o estados sin visual especifico. |

Esta separacion evita que `PortalEffect`, `InkPulse.anim` y `Movement.anim` dibujen cuerpos simultaneos.

### InkPulseState

| Estado | Efecto |
| --- | --- |
| `Idle` | Sin carga util. |
| `Charging` | La carga aumenta por graze o fuentes externas. |
| `Ready` | Puede activarse. |
| `Active` | Otorga impulso, puede resolver obstaculos como la red y activa feedback musical intenso. |

`RuntimeInkPulseState` conserva carga, estado activo y tiempo restante entre portales. `GameOver` lo reinicia.

El soundtrack dinamico no agrega una maquina de estado propia. `InkPulseMusicCrossfader` observa `InkPulseState.Active` y ajusta mezcla; `SoundtrackPitchProgression` observa `RuntimePlayerPace.ElapsedSpeedSeconds` y ajusta pitch. Ninguno gobierna input, dificultad, spawn ni dano.

### ShopEventState

`ShopEventState` gobierna el overlay temporal de suministros. No sustituye a `GameSessionState`: la partida sigue conceptualmente en `Playing`, aunque el manager puede congelar `Time.timeScale` mientras el contador de tienda avanza con tiempo real.

| Estado | Efecto |
| --- | --- |
| `Closed` | No hay oferta visible. |
| `Offering` | La tienda muestra gadget, precio y contador. La compra se intenta con `B`. |

### TutorialStep

`TutorialStep` pertenece a `TutorialDirector` y representa progresion pedagogica, no dificultad normal. Su contrato es observar o solicitar eventos de aprendizaje sin cambiar el significado de `RunEventState`, `GameSessionState` o `LevelSpawner`.

| Estado | Efecto |
| --- | --- |
| `Inactive` | Director presente pero sin secuencia activa. |
| `Movement` | Valida desplazamiento vertical dentro de `PlayerBoundaries`. |
| `Graze` | Valida carga parcial de Ink-Pulse por riesgo controlado. |
| `InkPulse` | Valida activacion real de Ink-Pulse. |
| `Shop` | Espera que la tienda temporal se presente. |
| `Gadgets` | Espera adquisicion o uso de un gadget. |
| `BossAndNet` | Espera resolucion pedagogica del evento de SS Carnage y red. |
| `Portal` | Espera entrada en transicion de portal. |
| `Completed` | Tutorial completado. |

El director puede activar compuertas sobre `LevelSpawner` y `BossEventDirector`, pero esos flags viven en `TutorialDirector`; no son excepciones dispersas en los sistemas de gameplay.

### SSCarnageAttackState

| Estado | Efecto |
| --- | --- |
| `Inactive` | Boss sin ataque activo. |
| `Warning` | Carnage avisa antes de desplegar red. |
| `DeployingNet` | Instanciacion de la red. |
| `NetActive` | La red decide resolucion o fallo; si cleanup elimina la red o el root del boss por quedar fuera de camara, el boss lo trata como resolucion para no bloquear `RunEventState.BossActive`. |
| `Resolved` | El jugador supero el evento. |
| `Failed` | El jugador fallo el evento. |
| `Exiting` | Carnage se retira hacia la derecha. |
| `Finished` | Evento terminado. |

### CameraEventMode

| Estado | Efecto |
| --- | --- |
| `Follow` | Seguimiento normal del jugador. |
| `WideEvent` | Vista amplia temporal para eventos. |
| `ReturningToFollow` | Interpolacion de vuelta al seguimiento; recupera el eje X mas rapido que el zoom para que el jugador no quede adelantado. |

### Ciclo visual de ZoneLightingController

No existe como enum formal porque todavia no bloquea input, no altera spawn y no coordina transiciones entre sistemas. Es un ciclo visual local:

| Fase conceptual | Efecto |
| --- | --- |
| Oscuro | `LayerBlack` usa `blackAlpha` y cubre el fondo. |
| Relevado | Cada `LightGrazeSource` declara una posicion de luz activa. |
| Composicion | `ZoneLightingController` genera una unica textura de oscuridad y usa la menor opacidad por pixel cuando dos luces se cruzan. |

Debe formalizarse como estado propio solo si en el futuro modifica reglas de spawn, IA, tutorial, audio adaptativo o interacciones de zona.

## Estados planificados

- `GadgetRuntimeState`

Nota sobre portales:
- `ScenePortal` ya implementa el cambio directo entre `ZonaEpipelagica` y `ZonaAbisopelagica`.
- `LevelSpawner` gobierna aparicion: `PostBossWindow` en zona principal y `AlwaysInterval` en `ZonaAbisopelagica`.
- Cruzar un portal conserva `RuntimeGadgetInventory` y `RuntimeInkPulseState`.
- Entrar en `GameSessionState.GameOver` reinicia ambos.
- La transicion visual actual se modela dentro de `PlayerRuntimeState.PortalTransition`. Solo debe escalar a una maquina `PortalTransitionState` separada si aparecen fases internas como entrada, fundido, carga asincronica o salida.

Nota sobre gadgets:
- El inventario ya existe como modelo runtime por posesion unica y slots.
- La asignacion de slots deriva del orden de adquisicion: `Gadget1` usa `Q`, `Gadget2` usa `W`.
- `Shell Shield` es pasivo y se consume antes de Game Over.
- `Ink-Bottle` es activo y fuerza `InkPulseState.Ready` cuando procede.
- Los desbloqueos permanentes de gadgets no son posesion runtime: `RunGadgetUnlockService` solo decide si pueden aparecer en `ShopEventState.Offering`.
- La tienda out-of-game no agrega estados de gadget; sus compras son skins o niveles de `permanentUpgrades`.
- `GadgetRuntimeState` se justifica cuando existan cooldowns, duraciones o animaciones de activacion.

## Lo que no es estado

Los boundaries no son una maquina de estado ni un parametro de progresion. Son contrato estructural de escena:
- `PlayerBoundaries/TopBoundary`
- `PlayerBoundaries/BottomBoundary`
- `CameraBoundaries/TopBoundary`
- `CameraBoundaries/BottomBoundary`

Si cambian, se ajusta la escena; no se agregan estados ni banderas para compensar.

## Regla base

- Si un sistema cambia comportamiento, habilita una interaccion o evita ambiguedad entre sistemas, merece estado propio.
- Si el sistema futuro puede resolverse con un `bool` sin perder claridad, no necesita otra maquina.
