# Auditoria de jerarquia runtime

## Proposito

Este documento define que script debe vivir en cada nodo principal de escena o prefab. Su funcion es evitar contradicciones: dos scripts gobernando el mismo movimiento, tags configurables que compiten con catalogos centrales, boundaries serializados en prefabs, o componentes heredados que ya no tienen responsabilidad.

La ficha completa de la escena principal esta en [ZonaEpipelagica.md](ZonaEpipelagica.md).

## Regla central

Un nodo debe tener un solo propietario por responsabilidad.

La regla de capas completa esta definida en [SoftwareArchitecture.md](SoftwareArchitecture.md): los managers, controllers, directors y spawners de sistema son duenos de flujo; los `...State` modelan fases; las especializaciones ejecutan comportamiento concreto; y los datos/catalogos no ejecutan gameplay.

- Estado global: `GameSessionController`.
- Progresion macro de run: `RunProgressionDirector`.
- Flujo de escenas: `SceneFlowController`.
- Movimiento del jugador: `PlayerMovement`.
- Ink-Pulse: `InkPulseController` y `RuntimeInkPulseState`.
- Score y pace runtime: `RunProgressionDirector` acumula; `RuntimeRunScore` conserva puntaje; `RuntimePlayerPace` conserva progresion de velocidad; `ScoreCounterDisplay` muestra puntaje.
- Estado runtime del jugador: `PlayerStateController`.
- Visuales del jugador: `PlayerVisualStateController` en el root decide entre `SquidVisual`, `InkPulseVisual` y `PortalVisual`.
- Visual del cuerpo del jugador: `SquidVisual` con `Squid.controller`.
- Visual del Ink-Pulse: `InkPulseVisual` con `InkPulseVisual.controller`.
- Visual de portal: `PortalVisual` con `PortalEffect.controller`.
- Colision del jugador: `PlayerCollision`.
- Carga por proximidad: `GrazeDetector`.
- Camara: `CameraController`.
- Movimiento horizontal del mundo y boundaries: `HorizontalTracker`.
- Limpieza de objetos fuera de camara: `DestroyOffscreen`.
- Resolucion de boundaries: `BoundaryReferenceResolver`.
- Spawn de enemigos, camarones, tienda y portales: `LevelSpawner`, parametrizado obligatoriamente por `ZoneSpawnProfile`.
- Soundtrack de zona: `Soundtrack` bajo `AudioRoot_*`; `InkPulseMusicCrossfader` gobierna mezcla normal/INK cuando existe y `SoundtrackPitchProgression` gobierna pitch progresivo por pace runtime.
- Tags compartidos: `GameplayTagCatalog`.
- Tags de enemigos: `EnemyTagCatalog`.
- Inventario runtime de gadgets: `PlayerGadgetInventory` y `RuntimeGadgetInventory`.
- Mercancia comprable: `GadgetShopItem`.
- Tienda temporal: `DealerFish` e `InGameShopManager`.
- Portales: `ScenePortal` detecta contacto; `SceneFlowController` decide destino.
- Iluminacion de zona: `ZoneLightingController` gobierna `LayerBlack`; `LightGrazeSource` declara muestras de luz visual con posicion, forma y titileo opcional.
- Economia persistente: `ShrimpRuntimeWallet` como API runtime y `PersistentPlayerProfile` como almacenamiento JSON.
- Boss SS Carnage: `BossEventDirector`, `SSCarnageController` y `SSCarnageNetWall`.
- UI de pausa: `PauseMenuManager`.
- UI de game over: `GameOverMenuManager`.
- Animacion de botones de menu: `MenuButtonAnimation`.
- Comics de lore: `LoreComicPresenter` en `LoreComicRoot`, con nodo visual `Comic`.

## Contrato del prefab del jugador

El jugador canonico vive en:

```text
Assets/Content/Prefabs/Player/BabySquid.prefab
```

Las escenas jugables no deben contener copias manuales del jugador. El nodo visible en escena sigue llamandose `Squid`, pero debe ser una instancia de `BabySquid.prefab`.

Jerarquia canonica:

```text
BabySquid
|-- GrazeZone
|-- SquidVisual
|-- InkPulseVisual
`-- PortalVisual
```

Reglas:
- El prefab conserva componentes internos del jugador: `PlayerMovement`, `InkPulseController`, `ShrimpCollector`, `PlayerCollision`, `PlayerGadgetInventory`, `PlayerStateController`, `PlayerVisualStateController`, `Rigidbody2D`, `CircleCollider2D`, `GrazeDetector` y visuales.
- El prefab base no serializa referencias a `GameSession`, camara, HUD, progression director ni boundaries.
- Cada instancia de escena llamada `Squid` debe tener asignadas en Inspector sus referencias externas: `GameSession`, `RunProgressionDirector`, `Main Camera` y `ChargeBar`.
- Los componentes conservan resolucion runtime como respaldo defensivo, no como fuente primaria de cableado.
- `ZonaAbisopelagica` puede agregar `LightGrazeSource` como override de instancia, porque la luz de esa zona es una capacidad ambiental especifica, no una propiedad base de BabySquid.
- Los cambios de collider, visual base, `GrazeZone`, `SquidVisual`, `InkPulseVisual`, `PortalVisual` o inventario deben hacerse en el prefab, no en copias de escena.
- Las skins futuras deben ser variantes visuales o prefab variants; no deben duplicar scripts de gameplay.
- Si se reconstruye el player, revisar manualmente que el prefab mantenga este contrato antes de propagarlo a escenas.

## Contrato de boundaries

Toda zona jugable debe contener estas jerarquias exactas:

```text
Boundaries
|-- CameraBoundaries
|   |-- TopBoundary
|   `-- BottomBoundary
`-- PlayerBoundaries
    |-- TopBoundary
    `-- BottomBoundary
```

`Boundaries` debe ser una instancia de `Assets/Content/Prefabs/World/Boundaries.prefab`. La instancia de cada zona puede tener overrides de posicion y colliders para representar su geometria concreta, pero no debe ser una copia local desempaquetada.

Cada `TopBoundary` y `BottomBoundary` debe tener un `Collider2D`. Los sistemas leen bounds fisicos internos mediante `BoundaryReferenceResolver`.

Reglas de mantenimiento:
- No serializar `topBorder`, `bottomBorder`, `playerTopBorder` ni `playerBottomBorder` en escenas o prefabs.
- No usar `fallbackMinY`, `fallbackMaxY`, `minY`, `maxY` serializados ni offsets manuales de top boundary como fuente de configuracion.
- No usar tags para encontrar boundaries.
- No crear una tercera jerarquia de limites para resolver un caso puntual.
- Si se cambia el tamano del escenario, se ajustan los colliders de la instancia prefab `Boundaries`; el codigo debe adaptarse solo.

## Propiedad de configuracion

Campos editables permitidos:
- Parametros de balance en managers/controladores duenos del sistema o en assets de datos consumidos por ellos, por ejemplo `RunProgressionDirector`, `InGameShopManager`, `SceneFlowController`, `CameraController`, `BossEventDirector`, `SSCarnageController` o `ZoneSpawnProfile`.
- Referencias tecnicas en managers de escena, cuando el manager es el dueno de la coordinacion.
- Datos propios de prefab, por ejemplo `GadgetShopItem.gadgetId` o `ShrimpValue.amount`.

Campos que no deben existir:
- Tags como strings locales si ya existe catalogo.
- Boundaries como referencias serializadas por componente.
- Parametros de balance en entidades puras como `PufferfishEnemy`, `DealerFish`, `ScenePortal` o `SSCarnageNetWall`.
- Parametros ajustables en servicios internos puros como `EnemySpawnSelector`, `SpawnPositionResolver`, `SpawnedObjectConfigurator`, `ShopOfferSelector` o `ShopPriceCalculator`.
- Canvas o nodos visuales autogenerados por scripts cuando la escena ya contiene la UI.
- Prefabs de enemigo genericos que compitan con `enemyProfiles`.

Los perfiles de datos serializados (`ZoneSpawnProfile`, `EnemySpawnProfile`, `PufferfishEnemyTuning`, `FishingRodEnemyTuning`, `ShopGadgetOffer`) pueden tener campos editables, pero no son duenos de comportamiento: existen para que el manager/controller dueno lea configuracion declarativa.

## Jerarquia de escenas jugables

La escena se organiza con roots de escena separados. `GameRoot` agrupa sistemas de gameplay y UI; `CameraRig`, `Enviroment` y `Audio` son roots hermanos para mantener la composicion espacial clara.

```mermaid
flowchart TD
    Scene[Scene Roots] --> GameRoot
    Scene --> CameraRig
    Scene --> Enviroment
    Scene --> Audio
    GameRoot --> Systems
    GameRoot --> Gameplay
    GameRoot --> Player
    GameRoot --> GameUIRoot
```

## Estandar de coordenadas

Las escenas `ZonaEpipelagica`, `ZonaAbisopelagica` y `ZonaTutorial` deben quedar centradas alrededor del origen de mundo:

- `CameraRig/Main Camera`: posicion inicial `(0, 0, -10)`.
- `GameRoot/Player/Squid`: posicion inicial `(-5, 0, 0)`, conservando el desplazamiento del jugador respecto de la camara.
- `GameRoot/Gameplay/Boundaries`: cerca del origen; las distancias reales las definen sus hijos `TopBoundary` y `BottomBoundary`.
- `Enviroment/Background`: posicion local `(0, 0, 0)`; sus capas visuales se ubican cerca de la camara con sus escalas propias.
- No escalar `GameRoot`, `Gameplay`, `CameraRig`, `Enviroment` ni `Audio` para corregir tamano. Los roots estructurales deben permanecer con escala `(1, 1, 1)`.
- Un escalado global distinto de `1` requiere una pasada de balance separada, porque afecta velocidades, offsets, colliders, camara, spawn y limpieza.

La revision previa a entrega debe confirmar estos puntos en las tres escenas jugables y verificar prefabs obligatorios, `ZoneSpawnProfile`, tags, layers, boundaries, `CleanUp`, `GameUIRoot`, ausencia de scripts faltantes y reglas especificas por zona como SS Carnage solo en `ZonaEpipelagica` y `ZoneLightingController` solo en `ZonaAbisopelagica`.

| Nodo | Script esperado | Responsabilidad |
| --- | --- | --- |
| `GameSession` | `GameSessionController`, `RunProgressionDirector` | Estado de partida y progresion temporal. |
| `GameSession` en `ZonaTutorial` | `TutorialDirector`, `TutorialPresentationController` | Progresion pedagogica y subsistema aislado de presentacion/freeze del tutorial. |
| `SceneFlow` | `SceneFlowController` | Carga de escenas y retorno al menu. |
| `LevelSpawner` | `LevelSpawner` con `zoneSpawnProfile` asignado | Autoridad de instanciacion de monedas, enemigos, tienda y portales. El asset `ZoneSpawnProfile` es la fuente autoritativa de balance; `EnemySpawnSelector`, `SpawnPositionResolver` y `SpawnedObjectConfigurator` son helpers internos sin nodo de escena. |
| `Main Camera` | `CameraController` | Seguimiento y eventos de camara. |
| `Boundaries` | Instancia de `Assets/Content/Prefabs/World/Boundaries.prefab`; `HorizontalTracker` | Mantener boundaries alineados con el avance horizontal. No debe ser copia local de escena. |
| `PlayerBoundaries` | hijos con `Collider2D` | Limites verticales del jugador. |
| `CameraBoundaries` | hijos con `Collider2D` | Limites verticales de camara. |
| `GameUIRoot` | `GameUIRoot` | Contrato de composicion de UI jugable: referencia EventSystem, HUD, vistas prefab y managers, sin gobernar estados ni gameplay. |
| `Squid` | Instancia de `BabySquid.prefab`; `PlayerMovement`, `InkPulseController`, `ShrimpCollector`, `PlayerCollision`, `PlayerGadgetInventory`, `PlayerStateController`, `PlayerVisualStateController` | Control completo del jugador sin copia manual por escena. |
| `GrazeZone` | `GrazeDetector` | Carga de Ink-Pulse por proximidad a amenazas. |
| `SquidVisual` | `SpriteRenderer`, `Animator` | Sprite del calamar y animacion de movimiento. |
| `InkPulseVisual` | `SpriteRenderer`, `Animator` | Sprite largo del impulso de tinta; visible solo cuando `PlayerVisualStateController` selecciona Ink-Pulse. |
| `PortalVisual` | `SpriteRenderer`, `Animator` | Transicion visual `PortalEffect`; visible solo durante `PlayerRuntimeState.PortalTransition`. |
| `CleanUp` | Instancia de `Assets/Content/Prefabs/World/CleanUp.prefab` | Contenedor canonico de limpieza fuera de camara. No debe ser copia local de escena. |
| `CleanUp/DestroyZone/GarbageCollector` | `DestroyOffscreen` | Seguir el borde izquierdo de camara, adaptar su alto a `CameraBoundaries` y destruir enemigos, camarones, collectibles y portales que ya salieron de pantalla. |
| `SSCarnageManager` | `BossEventDirector` | Disparar y coordinar eventos de boss. |
| `PauseMenuManager` | `PauseMenuManager` | Abrir, cerrar y cablear pausa. No contiene boton `Salir`; salir del juego pertenece solo a `MainMenu`. |
| `GameOverMenuManager` | `GameOverMenuManager` | Abrir, cerrar y cablear derrota. |
| `InGameShopManager` | `InGameShopManager` | Abrir tienda temporal y resolver compra. `ShopOfferSelector` y `ShopPriceCalculator` calculan oferta/precio sin nodo de escena. |
| `LoreComicRoot` | `LoreComicPresenter` | Mostrar vinetas narrativas de inicio, portal y derrota usando el nodo visual `Comic` existente. |
| Botones de pausa/game over | `MenuButtonAnimation` | Animacion interactiva visual fija del boton; no expone parametros por boton. |
| Fondo burbujas UI | `MenuBubbles` | Movimiento decorativo compartido. |
| `InkBar` en zonas jugables | `ChargeBar`, `InkBarFillPresenter` | Barra Ink-Pulse canonica para `ZonaEpipelagica`, `ZonaAbisopelagica` y `ZonaTutorial`. La orientacion y presentacion visual pertenecen al prefab `InkBar`. |
| `TutorialPresentationOverlay` en `ZonaTutorial` | `Canvas`, `CanvasGroup`, `Dimmer` con `Image` | Oscurecimiento temporal durante `TutorialPhase.Presentation`; no bloquea raycasts ni crea prompts. |
| `Score` | `ScoreCounterDisplay` | Puntaje runtime de la run. |
| `ShrimpCounter` | `ShrimpCounterDisplay` | Saldo persistente de camarones del perfil. |
| `GadgetSlots` | `GadgetInventoryHud` | Slots de inventario y teclas de gadgets activos. |

## Jerarquia especifica de ZonaAbisopelagica

`ZonaAbisopelagica` comparte el contrato de `ZonaEpipelagica`, pero agrega iluminacion ambiental:

| Nodo | Script esperado | Responsabilidad |
| --- | --- | --- |
| `EnviromentRoot_ZonaAbisopelagica/ZoneLightingController` | `ZoneLightingController` | Oscurecer la zona y componer las zonas locales de luz. |
| `EnviromentRoot_ZonaAbisopelagica/ZoneLightingController/LayerBlack` | `SpriteRenderer` | Capa negra semitransparente que cubre camara y recibe la textura compuesta de oscuridad. |
| `EnviromentRoot_ZonaAbisopelagica/Layer1..Layer5` | `ParallaxLayer` | Capas de fondo abisal con reciclaje, culling por camara y limite de tiles generados. |
| `FlappyBossManager` | `BossEventDirector` | Disparar `UnknownBoss` / `FlappyBoss` como boss propio de la zona abisal. |
| `SSCarnageManager`, `SSCarnage`, `BossNetWall` | ninguno | No deben existir en esta zona mientras SS Carnage no sea parte de su diseno. |

`LayerBlack` debe quedar sobre fondos y bajo entidades de gameplay. En el modo actual usa una textura generada por `ZoneLightingController` y `SpriteRenderer.maskInteraction = None`. El modo `VisibleOutsideMask` queda reservado para el fallback legacy con `SpriteMask`.

## Prefabs runtime

| Prefab | Script esperado | Tag esperado | Layer esperada |
| --- | --- | --- | --- |
| `BabySquid` | `PlayerMovement`, `InkPulseController`, `ShrimpCollector`, `PlayerCollision`, `PlayerGadgetInventory`, `PlayerStateController`, `PlayerVisualStateController` | `Player` | `Player` |
| `PezGlobo` | `PufferfishEnemy` | `EnemyPezGlobo` | `Enemy` |
| `Mina` | ninguno por ahora | `EnemyMina` | `Enemy` |
| `CanaPescar` | `FishingRodEnemy` | `EnemyCanaPescar` | `Enemy` |
| `ShrimpCoin` | `ShrimpValue` | `Shrimp` | `Collectible` |
| `ShrimpCoinX10` | `ShrimpValue` | `Shrimp` | `Collectible` |
| `ShellShield` | `GadgetShopItem` | `Untagged` | segun UI/prefab |
| `InkBottle` | `GadgetShopItem` | `Untagged` | segun UI/prefab |
| `DealerFish` | `DealerFish` | `Collectible` | `Collectible` |
| `DealerFish_ZonaAbisopelagica` | `DealerFish` | `Collectible` | `Collectible` |
| `ScenePortal` | `ScenePortal` | `Portal` | `Collectible` |
| `SSCarnage` | `SSCarnageController`, `BoxCollider2D` trigger para cleanup | `SSCarnage` | `Boss` |
| `BossNetWall` | `SSCarnageNetWall`, collider trigger activo para cleanup | `SSCarnage` | `Boss` |
| `UnknownBoss` | `FlappyBossController` | `Boss` | `Boss` |
| `CleanUp` | `DestroyOffscreen` en `DestroyZone/GarbageCollector` | root `Untagged`; hijo `DestroyZone` | root `Default`; hijos `DestroyZone` |
| `Boundaries` | `HorizontalTracker` en root, colliders en `TopBoundary`/`BottomBoundary` | `Untagged` | `Boundary` |

La mina no requiere script propio en esta entrega porque su comportamiento es estatico y su aparicion vive en el algoritmo de spawn. La cana ya tiene `FishingRodEnemy`; su temporizacion de aparicion pertenece a `LevelSpawner`, pero su caida vertical pertenece al prefab.

`CanaPescar` puede tener `Rope` y `Visual` escalados para lectura visual. Ese tamano pertenece al prefab: la identidad jugable se conserva en el root (`EnemyCanaPescar` / `Enemy`) y los hijos visuales permanecen sin tag logico propio. El cleanup debe evaluar bounds agregados de colliders/renderers para no destruir la cana hasta que todo su volumen haya pasado la distancia segura.

## Prefabs UI

La barra de Ink-Pulse y el resto de HUD/menus principales existen como prefabs de UI. `ZonaEpipelagica`, `ZonaAbisopelagica` y `ZonaTutorial` deben consumir estos assets como instancias de prefab, no como copias locales desempaquetadas.

| Prefab | Uso | Componentes esperados |
| --- | --- | --- |
| `Assets/Content/Prefabs/UI/HUD/InkBar.prefab` | Todas las zonas jugables | `ChargeBar`, `InkBarFillPresenter`, `Mask`/relleno visual, `Animator` en `InkBarEffectVisual` |
| `Assets/Content/Prefabs/UI/HUD/GadgetSlots.prefab` | HUD comun | `GadgetInventoryHud`, slots `Gadget1`/`Gadget2`, textos `Q`/`W` |
| `Assets/Content/Prefabs/UI/HUD/ShrimpCounter.prefab` | HUD comun | `ShrimpCounterDisplay`, icono y texto de cantidad |
| `Assets/Content/Prefabs/UI/HUD/ScoreCounter.prefab` | HUD comun | `ScoreCounterDisplay` |
| `Assets/Content/Prefabs/UI/Menus/PauseMenu.prefab` | Overlay de pausa | `PauseCanvas`, `CanvasGroup`, botones y animaciones visuales; sin manager dentro del prefab |
| `Assets/Content/Prefabs/UI/Menus/GameOverMenu.prefab` | Overlay de derrota | `GameOverCanvas`, `CanvasGroup`, botones y animaciones visuales; sin manager dentro del prefab |
| `Assets/Content/Prefabs/UI/Menus/InGameShopMenu.prefab` | Tienda temporal in-run | `InGameCanvas`, `CanvasGroup`, `Comprar`, `Gadget`, `Precio`, `B`, `SinSaldo`; sin manager dentro del prefab |
| `Assets/Content/Prefabs/UI/Menus/LoreComic.prefab` | Overlay narrativo | `LoreComicRoot`, `LoreComicPresenter`, `Comic`, `CanvasGroup`, `Dimmer`, `Vineta`, `ContinuarBoton` |
| `Assets/Content/Prefabs/UI/Menus/OptionsMenu.prefab` | Opciones globales | `OptionsMenu`, Canvas propio, `OptionsPanel`, fondo `Background`/`Fondo`, `OptionsMenuManager`; instancia como root separado de escena |

Reglas:
- La UI jugable debe colgar de `GameUIRoot`, no de un root generico `UI`.
- `GameUIRoot` es un contrato de composicion: expone referencias, pero no instancia prefabs ni decide estados.
- `ChargeBar` no debe contener reglas de layout de una variante concreta.
- `InkBarFillPresenter` no debe conocer gameplay; solo interpreta el valor recibido.
- Los prefabs UI no deben serializar referencias a `Squid`, `InkPulseController`, sesion ni managers.
- Las escenas asignan el `ChargeBar` al `InkPulseController` del jugador.
- Los botones de prefabs UI no deben serializar eventos persistentes hacia managers externos de escena.
- Los botones que llaman a un componente del mismo prefab pueden conservar `OnClick` persistente autocontenido.
- Los managers de escena pueden cablear listeners en runtime solo como respaldo defensivo; el contrato preferido es que referencias y acciones relevantes sean visibles o serializadas para auditoria.
- Los managers de escena no deben desactivar listeners persistentes del Inspector. No usar `SetPersistentListenerState` ni helpers tipo `DisablePersistentOnClick`.
- `PauseMenuManager`, `GameOverMenuManager` e `InGameShopManager` conservan las referencias de escena hacia las instancias visuales. Tambien pueden resolver referencias por nombre si el prefab visual se ubica bajo su jerarquia.
- Cada zona debe conservar `GameRoot/GameUIRoot/HUD/InkBar` como instancia de `Assets/Content/Prefabs/UI/HUD/InkBar.prefab`.

Contrato de `LoreComic`:
- `MainMenu` debe contener una instancia `LoreComicRoot` para el comic de inicio.
- `GameRoot_ZonaEpipelagica`, `GameRoot_ZonaAbisopelagica` y `GameRoot_ZonaTutorial` deben contener una instancia `LoreComicRoot` para portales y derrotas.
- `LoreComicRoot` puede estar activo aunque `Comic` este oculto por `CanvasGroup`; el componente debe permanecer activo para ejecutar corrutinas.
- Todo `LoreComic.prefab` debe estar en layer `UI`.
- Las vinetas finales se asignan en `LoreComicPresenter.entries`; los sprites deben conservar `.meta` estable bajo `Assets/Content/Art/ComicLore/`.
- Las entradas de tienda in-game esperadas son `ShopInGameFirst`, `ShopInGameLastPurchased` y `ShopInGameLastNoPurchase`.

## Spawn de enemigos

`LevelSpawner` no usa un prefab generico heredado ni campos legacy de balance en escena. Todo enemigo nace desde `ZoneSpawnProfile.enemyProfiles`. Cada entrada define:

- `prefab`: prefab concreto a instanciar.
- `enemyTag`: tag logico del enemigo.
- `baseWeight`: peso relativo de aparicion.
- `minIntensity`: intensidad minima de run.
- `spawnIntervalMultiplier`: modificador local del intervalo tras ese spawn.

Despues de instanciar, `LevelSpawner` llama a `SpawnedObjectConfigurator`, que aplica tag con `EnemyTagCatalog.ApplyEnemyTag()`, asigna capa `Enemy` de forma recursiva y entrega `EnemySpawnContext`.
Los comportamientos de enemigos reciben `EnemySpawnContext`; sus parametros de balance viven en `ZoneSpawnProfile`, no en el prefab.
En `ZonaAbisopelagica`, `SpawnedObjectConfigurator` tambien garantiza `LightGrazeSource` en enemigos, camarones, `DealerFish` y portales instanciados, porque `LightGrazeSource.EnsureOn()` solo actua si existe `ZoneLightingController`.

## Spawn de tienda

`LevelSpawner` instancia `DealerFish` por intervalo independiente del spawn regular:
- aparece por la derecha de la camara;
- usa capa `Collectible`;
- usa tag `Collectible`;
- se ubica en la zona inferior configurable del rango definido por `PlayerBoundaries`;
- agenda cada aparicion con intervalo base multiplicado por un factor aleatorio de tienda;
- abre `InGameShopManager` al colisionar con el jugador.

`ZonaAbisopelagica` usa `DealerFish_ZonaAbisopelagica.prefab` desde su `ZoneSpawnProfile`. Es una variante visual del dealer base: conserva tag, layer, collider y script, pero `Visual` y `VisualSupport` usan RGB `135,135,135` para integrarse con la oscuridad abisal. Las demas zonas siguen usando `DealerFish.prefab`.

## Spawn de portales

`LevelSpawner` instancia `ScenePortal` por politica de aparicion:
- aparece por la derecha de la camara;
- usa capa `Collectible`;
- usa tag `Portal`;
- se ubica dentro del rango definido por `PlayerBoundaries`;
- `ScenePortal` detecta la colision y `SceneFlowController` resuelve el destino.

Configuracion actual:
- `ZonaEpipelagica`: `PortalSpawnPolicy.PostBossWindow`, primer portal inmediato durante post-boss.
- `ZonaAbisopelagica`: `PortalSpawnPolicy.PostBossWindow`, tirada unica tras el boss usando `firstPortalSpawnDelay` y `postBossPortalSpawnChance`.

## Light graze visual

`LightGrazeSource` no es un manager y no tiene parametros de balance mecanico. Es una declaracion runtime de capacidad visual.

Reglas:
- El balance visual vive solo en `ZoneLightingController`.
- La instancia `Squid` de `BabySquid.prefab` debe tener `LightGrazeSource` solo en `ZonaAbisopelagica`.
- Las entidades spawneadas reciben `LightGrazeSource` por `SpawnedObjectConfigurator` solo en zonas con `ZoneLightingController`.
- En modo compuesto, `LightGrazeSource` no crea renderers visibles: solo registra su posicion para que `ZoneLightingController` regenere una unica textura de oscuridad.
- Cada fuente puede declarar `grazeAnchor`, `lightShapeScale` y titileo para ajustar lectura local de una entidad concreta.
- El fallback legacy puede crear `LightGrazeMask` y `LightGrazeFeather` como hijos runtime si se desactiva `useCompositeLightOverlay`.
- `GrazeDetector` y `LightGrazeSource` no deben compartir estado ni cargar el mismo recurso.

## Scripts retirados o reemplazados

| Script anterior | Estado | Motivo |
| --- | --- | --- |
| `CameraFollowHorizontal` | eliminado | Duplicaba responsabilidades de `CameraController` y `HorizontalTracker`. |
| `PauseButtonAnimation` | reemplazado por `MenuButtonAnimation` | Su uso ya no es exclusivo del menu de pausa. |
| `PauseBubbles` | eliminado | `MenuBubbles` cubre la animacion decorativa compartida. |
| `GadgetPickup` | reemplazado | Los gadgets se compran, no se recogen directamente. |
| `ShopPickup` | reemplazado por `DealerFish` | El ente de tienda tiene identidad propia. |
| `PlayerInkPulseVisualController` | reemplazado en jerarquia por `PlayerVisualStateController` | Ink-Pulse ya no puede ser el unico dueno visual porque portal e Ink-Pulse compiten por ocultar el cuerpo. |

## Invariantes de mantenimiento

- No agregar un segundo script de movimiento a `Main Camera`.
- No agregar un fallback generico de enemigo al spawner si ya existen perfiles.
- No dejar `LevelSpawner.zoneSpawnProfile` vacio en zonas jugables.
- Revisar manualmente los contratos de escena, prefabs y perfiles de spawn antes de cerrar una refactorizacion.
- No bloquear el spawn regular durante `BossActive`; el evento debe duplicar frecuencia, no detener obstaculos.
- No usar `LevelSpawner` para lanzar anzuelos desde el SS Carnage; los ataques que nacen del boss deben vivir en el controlador/prefab del boss.
- No serializar tags de enemigos en `PlayerCollision`, `GrazeDetector` o `DestroyOffscreen`.
- No declarar tags compartidos (`Player`, `Shrimp`, `Collectible`, `Portal`) como strings locales en scripts de gameplay.
- No posicionar `GarbageCollector` manualmente para balancear limpieza; `DestroyOffscreen` se alinea por camara en runtime.
- No desempaquetar ni duplicar `Boundaries` en escenas jugables; usar siempre `Assets/Content/Prefabs/World/Boundaries.prefab`.
- No desempaquetar ni duplicar `CleanUp` en escenas jugables; usar siempre `Assets/Content/Prefabs/World/CleanUp.prefab`.
- No dimensionar `CleanUp` desde el viewport ni desde la ortografica de camara; el alto valido es la distancia interna entre `CameraBoundaries/BottomBoundary` y `CameraBoundaries/TopBoundary`.
- No desactivar colliders de objetos que deben permanecer visibles y ser limpiados por `DestroyOffscreen`; usar flags internos para ignorar nuevas interacciones.
- No declarar Game Over por colision sin consultar antes `PlayerGadgetInventory` para `Shell Shield`.
- No fijar `W` o `Q` desde el prefab de gadget; el slot visual se asigna por orden de adquisicion.
- Mantener `Gadget1 = Q` y `Gadget2 = W` tanto en HUD como en input.
- No autogenerar nodos visuales de `GadgetSlots` desde `GadgetInventoryHud`; la UI pertenece al canvas de escena.
- No autogenerar `LoreComicRoot`, Canvas `Comic` ni vinetas desde runtime; deben existir como prefab/instancia con referencias serializadas.
- No stackear gadgets: cada `GadgetId` existe como posesion unica.
- No comprar desde tienda sin pasar por `ShrimpRuntimeWallet.TrySpend`.
- No modificar JSON de `Application.persistentDataPath/db` directamente desde sistemas de gameplay; usar `PersistentPlayerProfile`, `ShrimpRuntimeWallet` o `LocalLeaderboardRepository`.
- No guardar settings en `player-profile.json`, `player-records.json`, `unlockables-catalog.json` ni `local-leaderboard.json`; volumen, brillo, pantalla y dificultad pertenecen a otro almacenamiento.
- No autogenerar canvas de tienda desde `InGameShopManager`.
- No volver a introducir un root generico `UI` en escenas jugables; usar `GameUIRoot`.
- No entregar gadgets por colision directa: los gadgets se compran desde `InGameShopManager`.
- No vender gadgets desde `ShopMenu` ni desde la tienda out-of-game; fuera de la run solo se habilita su elegibilidad mediante `RunGadgetUnlockService`.
- No permitir activacion manual de Ink-Pulse mientras `InGameShopManager` esta en `ShopEventState.Offering`.
- No usar `LightGrazeSource` para cargar Ink-Pulse; su unica consecuencia es visual y pertenece a `ZoneLightingController`.
- En `ZonaAbisopelagica`, `BossEventDirector` solo es valido si el nodo es `FlappyBossManager` y su prefab es `UnknownBoss`; no debe instanciar SS Carnage ni `BossNetWall`.
- No dejar portales fijos `PortalTo...` en escena; los portales nacen desde `LevelSpawner`.
- No usar tag `Shrimp` ni `Collectible` en portales; deben usar `Portal`.
- No cargar zonas desde scripts de enemigo, tienda o HUD; el contacto pertenece a `ScenePortal`, pero las rutas pertenecen a `SceneFlowController`.
- No activar `Ink-Bottle` si el Ink-Pulse ya esta en `Ready` o `Active`; no debe consumirse sin efecto.
- No poner logica de boss en el prefab de red que ya pertenece a `SSCarnageController`.
- No mezclar la animacion del cuerpo y el efecto largo de Ink-Pulse en el mismo `Animator`; `SquidVisual` y `InkPulseVisual` deben mantenerse separados.
- No dejar visible `SquidVisual` durante `InkPulseState.Active` si `InkPulse.anim` ya contiene al cuerpo del calamar.
- No dejar visible `SquidVisual` ni `InkPulseVisual` durante `PlayerRuntimeState.PortalTransition`; en ese estado solo debe verse `PortalVisual`.
- No editar una copia local de `Squid` como si fuera fuente canonica; los cambios estructurales del jugador deben aplicarse en `BabySquid.prefab`.
- Si un nuevo enemigo necesita comportamiento, debe tener prefab, tag, perfil de spawn y script propio documentados juntos.
