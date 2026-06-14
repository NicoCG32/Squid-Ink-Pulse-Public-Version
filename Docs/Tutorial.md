# Tutorial

## Alcance

`ZonaTutorial` es una escena futura para ensenar mecanicas de forma gradual antes de entrar al flujo completo de run. Ya esta registrada como escena conocida por `SceneFlowController`, pero todavia no debe considerarse una zona de gameplay normal.

## Secuencia prevista

El tutorial debe presentar las mecanicas en este orden:

1. Movimiento vertical dentro de `PlayerBoundaries`.
2. Graze como riesgo controlado para cargar Ink-Pulse.
3. Activacion de Ink-Pulse.
4. Aparicion de tienda temporal.
5. Compra y uso de gadgets.
6. Evento de boss con SS Carnage y pared/red.
7. Portal hacia zona de juego.

## Controlador implementado

`ZonaTutorial` contiene `TutorialDirector` en el nodo `GameSession`. Este director formaliza la secuencia pedagogica mediante `TutorialStep` y observa sistemas existentes sin meter excepciones de tutorial en el spawner, la progresion o el jugador.

Responsabilidades:
- Activar pasos en orden.
- Bloquear o habilitar mecanicas segun el paso activo.
- Permitir bloquear o habilitar `LevelSpawner` y `BossEventDirector` desde parametros del propio director.
- Medir criterios de avance, por ejemplo moverse, cargar Ink-Pulse, comprar un gadget o cruzar un portal.
- Terminar cargando `ZonaEpipelagica` mediante `SceneFlowController`.

Pasos formales:

| Paso | Criterio actual |
| --- | --- |
| `Movement` | El jugador se desplaza verticalmente al menos `movementRequiredVerticalDelta`. |
| `Graze` | `InkPulseController.ChargeRatio` llega a `grazeRequiredChargeRatio`. |
| `InkPulse` | Se activa Ink-Pulse. |
| `Shop` | `InGameShopManager` entra en `ShopEventState.Offering`. |
| `Gadgets` | El inventario runtime registra algun gadget o la compra notifica al director. |
| `BossAndNet` | `RunProgressionDirector` llega a `PostBossWindow` o se llama `NotifyBossTutorialResolved`. |
| `Portal` | La progresion entra en `Transitioning` o se llama `NotifyPortalEntered`. |
| `Completed` | La secuencia pedagogica termino. |

El director tambien expone metodos manuales (`NotifyShopPresented`, `NotifyGadgetAcquiredOrUsed`, `NotifyBossTutorialResolved`, `NotifyPortalEntered`) para que futuros overlays, spawns dirigidos o botones de QA puedan avanzar sin acoplarse a detalles internos.

Reglas:
- No meter excepciones de tutorial en `LevelSpawner` si el comportamiento solo existe para ensenar.
- No alterar `RunProgressionDirector` para pasos pedagogicos.
- Mantener `PlayerBoundaries` y `CameraBoundaries` como contrato obligatorio tambien en tutorial.
- Usar una instancia de `Assets/Content/Prefabs/Player/BabySquid.prefab`; no duplicar ni simplificar al jugador para tutorial.
- Si el tutorial requiere una posicion inicial pedagogica, debe resolverla mediante un `PlayerSpawnPoint` o `TutorialDirector`, no editando el prefab.
- Si un paso necesita texto, marcador o overlay, debe pertenecer a UI de tutorial, no a los prefabs de enemigos.

## Menus relacionados

`ShopMenu` y `OptionsMenu` ya existen como escenas preparadas para trabajo posterior:
- `ShopMenu`: tienda global de skills, skins y bonificaciones.
- `OptionsMenu`: volumen, pantalla y dificultad.

Estos menus no reemplazan la tienda temporal in-game. Son flujos de menu, no eventos de mundo.
