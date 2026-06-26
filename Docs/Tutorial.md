# Tutorial

## Alcance

`ZonaTutorial` ensena el loop principal mediante una secuencia dirigida y testeable. No es una zona normal de gameplay: funciona como onboarding y como prueba integrada de movimiento, graze, Ink-Pulse, tienda temporal, gadgets, SS Carnage, portal local y Game Over.

El tutorial no implementa textos, prompts ni senalizacion visual. Cualquier indicacion futura debe conectarse a los eventos de `TutorialDirector`, sin duplicar el sistema.

## Controlador

`TutorialDirector` vive en `GameRoot_ZonaTutorial.prefab`, sobre el nodo `GameSession`. Es el unico propietario de la progresion pedagogica.

`TutorialPresentationController` vive en el mismo nodo y es el subsistema aislado de presentacion. Su responsabilidad es congelar el juego durante `TutorialPhase.Presentation`, oscurecer levemente la pantalla y suprimir activacion de Ink-Pulse mientras el jugador esta leyendo/observando la fase.

`TutorialTaskHudController` vive bajo `TutorialPresentationOverlay/TutorialTaskHUD`. Su contrato es exclusivamente activar un nodo por `TutorialStep` durante `TutorialPhase.Presentation`. No dibuja textos ni crea visuales por codigo: cada hijo del HUD es un placeholder para que se agreguen animaciones desde Unity.

Responsabilidades:
- Avanzar por `TutorialStep` en orden.
- Instanciar amenazas, camarones, tiendas y portal dirigidos mediante prefabs serializados.
- Bloquear `LevelSpawner` y `BossEventDirector` durante la secuencia para evitar ruido de spawns normales.
- Observar sistemas existentes: `InkPulseController`, `PlayerCollision`, `InGameShopManager`, `RuntimeGadgetInventory`, `RunProgressionDirector` y `SSCarnageController`.
- Forzar ofertas tutoriales de `InGameShopManager` sin cambiar la tienda normal.
- Resolver el portal como transicion local dentro de la misma escena.
- Dividir cada paso en subfase de presentacion y subfase de practica.
- Mantener el score runtime en cero mientras el tutorial esta activo.
- Emitir eventos de fase para que subsistemas tutoriales aislados reaccionen sin meter excepciones en gameplay normal.

No debe:
- Crear un sistema paralelo de tutorial.
- Meter excepciones pedagogicas en `LevelSpawner`, jugador, boss o tienda.
- Crear canvas, textos o prompts por codigo.
- Cambiar arte visual del usuario.

## Secuencia

Cada paso entra primero en `TutorialPhase.Presentation` y luego en `TutorialPhase.Practice`.

Durante `Presentation`:
- `TutorialDirector` entra a `TutorialPhase.Presentation`.
- `TutorialPresentationController` congela `Time.timeScale` si `freezeGameplay` esta activo.
- `TutorialPresentationController` muestra `TutorialPresentationOverlay` con un dimmer semitransparente.
- `TutorialTaskHudController` activa el hijo asociado al `TutorialStep` actual y reinicia su `Animator` si existe.
- `defaultPresentationSeconds` define la duracion base; actualmente `7`.
- `InkPulseController` queda temporalmente suprimido por `TutorialPresentationController` para evitar activaciones durante la presentacion.
- Se emiten `onPresentationStarted` y `onPhaseStarted`.

Durante `Practice`:
- Se restaura el `Time.timeScale` previo.
- Se preparan los spawns/objetivos mecanicos del paso.
- `defaultPracticeSeconds` define la ventana base; actualmente `10`.
- Se emiten `onPracticeStarted` y `onPhaseStarted`.
- Si el jugador cumple la condicion antes del tiempo, el paso avanza.
- Si expira el tiempo, solo avanzan automaticamente los pasos no bloqueantes. Compras, portal, Game Over y resoluciones criticas siguen esperando su condicion.

`stepTimingOverrides` permite sobreescribir duracion de presentacion, duracion de practica y autoavance por paso desde Inspector.

| Paso | Objetivo | Condicion de avance |
| --- | --- | --- |
| `Movement` | Movimiento basico. | El jugador se desplaza verticalmente al menos `movementRequiredVerticalDelta`. |
| `GrazeCharge` | Cargar Ink-Pulse con graze. | `InkPulseController.ChargeRatio >= grazeRequiredChargeRatio`. En tutorial el prefab usa `1`. |
| `InkPulseObstacle` | Usar Ink-Pulse contra una amenaza. | `PlayerCollision` informa que una amenaza fue ignorada por Ink-Pulse activo. |
| `CollectShrimps10` | Recolectar 10 camarones. | La diferencia de `ShrimpRuntimeWallet.TotalShrimp` contra el baseline del paso llega a `requiredShrimpCount`. |
| `FirstShopOpen` | Abrir primera tienda. | `InGameShopManager` entra en `ShopEventState.Offering`. |
| `BuyInkBottle` | Comprar Ink Bottle. | `RuntimeGadgetInventory.HasGadget(InkBottle)`. |
| `InkBottleBarrier` | Usar Ink Bottle para preparar Ink-Pulse y superar una barrera de enemigos. | `PlayerCollision` informa que una amenaza de la barrera fue ignorada por Ink-Pulse activo. |
| `CarnageIntro` | Presentar SS Carnage y red. | `SSCarnageController` llega a `NetActive`. |
| `CarnageInkPulseAssist` | Pausa/asistencia mecanica breve. | Ink-Pulse queda listo o activo; si hace falta, `TryForceReady()` se ejecuta tras `inkPulseAssistDelaySeconds`. |
| `CarnageInkPulseResolve` | Superar la red con Ink-Pulse. | La progresion llega a `PostBossWindow` o Carnage queda `Resolved`/`Finished`. |
| `SecondShopOpen` | Abrir segunda tienda. | `InGameShopManager` entra en `Offering`. |
| `BuyShellShield` | Comprar Shell Shield. | `RuntimeGadgetInventory.HasGadget(ShellShield)`. |
| `ProtectedHitSetup` | Provocar enemigo protegido. | Se instancia amenaza dirigida y se da una ventana corta de armado. |
| `ProtectedHitResolved` | Validar segunda oportunidad. | `PlayerCollision` informa bloqueo por Shell Shield, o Shell Shield ya fue consumido. |
| `PortalSpawn` | Crear portal local. | El portal dirigido existe. |
| `PortalEnter` | Entrar al portal. | `ScenePortal.ConfigureLocalTransition()` marca entrada local; no se carga escena. |
| `VisualZoneShift` | Cambiar a segunda zona local. | Se activan/desactivan `firstZoneVisualRoots` y `secondZoneVisualRoots` si estan asignados. |
| `FinalEnemy` | Provocar enemigo final. | Se instancia amenaza final y se arma la fase. |
| `FinalDeath` | Morir sin Shell Shield. | `PlayerCollision` informa Game Over o `GameSessionController.IsGameOver`. |
| `Completed` | Tutorial terminado. | La secuencia llego a Game Over y completo sus objetivos. |

## Prefab y escena

`GameRoot_ZonaTutorial.prefab` debe mantener:
- `initialStep = Movement` serializado como valor `10`.
- `scorePerSecond = 0` en `RunProgressionDirector`.
- `suppressScoreDuringTutorial = true`.
- `defaultPresentationSeconds = 7`.
- `defaultPracticeSeconds = 10`.
- `presentationController` asignado al `TutorialPresentationController` del mismo nodo.
- `TutorialPresentationController.freezeGameplay = true`.
- `TutorialPresentationController.presentationAlpha = 0.35`.
- `TutorialPresentationOverlay` bajo `GameUIRoot`, con root activo solo durante presentacion y `Dimmer` negro con `CanvasGroup` alpha inicial `0`.
- El alpha de oscurecimiento debe aplicarse al `Dimmer`, no al root del overlay, para que `TutorialTaskHUD` no herede transparencia.
- `TutorialPresentationOverlay/TutorialTaskHUD` con un hijo por `TutorialStep`; esos hijos son placeholders visuales editables por Inspector.
- `grazeRequiredChargeRatio = 1`.
- `inkBottleBarrierEnemyPrefab` asignado a una amenaza y `inkBottleBarrierEnemyCount` ajustable desde Inspector.
- `emptyInkPulseBeforeInkBottleBarrier = true` para que Ink Bottle sea necesario en esa prueba dirigida.
- `controlLevelSpawner = true` y `levelSpawnerEnabledFromStep = Completed`.
- `controlBossDirector = true` y `bossDirectorEnabledFromStep = Completed`.
- Prefabs dirigidos asignados: PezGlobo para graze, Mina para amenazas, ShrimpCoin, DealerFish y ScenePortal.
- `tutorialSpawnParent` apuntando al padre de spawns y `tutorialPortalParent` al padre de portales.

`ZonaTutorial.unity` debe mantener overrides de escena hacia `CameraRig`:
- `TutorialDirector.gameplayCamera`.
- `LevelSpawner.spawnCamera`.
- `BossEventDirector.spawnCamera`.
- `BossEventDirector.eventCameraController`.

`firstZoneVisualRoots` y `secondZoneVisualRoots` son puntos de conexion visual. Si estan vacios, el flujo mecanico avanza igual; cuando se agregue arte de segunda zona, debe asignarse por Inspector.

## Gadgets

Ink Bottle es un gadget activo de run, no un buff temporal. Al usarse desde `PlayerGadgetInventory` fuerza Ink-Pulse a `Ready` si puede, pero no se consume durante la run.

Shell Shield es pasivo y se consume automaticamente al bloquear un impacto que causaria Game Over.

## Hooks publicos

`TutorialDirector` conserva metodos manuales para integraciones futuras:
- `NotifyShopPresented()`
- `NotifyGadgetAcquiredOrUsed()`
- `NotifyBossTutorialResolved()`
- `NotifyPortalEntered()`

Estos hooks son para overlays, herramientas QA o eventos visuales futuros; no reemplazan las condiciones mecanicas principales. Las notificaciones externas que resuelven tienda, boss o portal solo avanzan durante `TutorialPhase.Practice`, nunca durante `Presentation`.
