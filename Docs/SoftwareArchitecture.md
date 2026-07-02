# Arquitectura de software

## Proposito

Este documento formaliza la arquitectura runtime de Squid Ink-Pulse. Su objetivo es que cada sistema tenga un propietario claro, que las maquinas de estado sean visibles, y que el crecimiento hacia tienda permanente, tutorial, skins, zonas y enemigos no produzca scripts redundantes o contradictorios.

El informe historico en `Docs/Reports/` no se reescribe. Este documento describe el contrato arquitectonico de la entrega.

## Tesis arquitectonica

La base del proyecto se organiza por dominios. Dentro de cada dominio se respeta esta direccion conceptual:

```text
Dominio
`-- Orquestadores: ...Manager, ...Controller, ...Director o spawner de sistema
    `-- Estado formal: ...State, runtime state stores o snapshots de lectura
        `-- Especializaciones: entidades, efectos, displays, fuentes, valores y adaptadores
            `-- Datos locales: catalogos, perfiles, tuning, repositorios y definiciones
```

La frase "el estado se encarga de la especializacion" debe entenderse de forma precisa: el estado formal modela la fase del sistema; el controlador o manager es quien ejecuta la transicion y aplica sus efectos sobre las especializaciones. En C#, muchos `...State` son `enum` puros y por tanto no deben llamar directamente a componentes Unity.

## Capas

### 1. Dominios

Un dominio es una carpeta bajo `Assets/Implementation/Code/` con una responsabilidad funcional principal.

| Dominio | Responsabilidad |
| --- | --- |
| `Core` | Sesion, escenas, progresion, camara, boundaries e infraestructura transversal. |
| `Player` | Movimiento, Ink-Pulse, colisiones, inventario, visuales y perfil persistente. |
| `Spawning` | Aparicion de enemigos, camarones, tienda in-run y portales. |
| `Enemies` | Comportamiento propio de enemigos concretos. |
| `Bosses` | Orquestacion de eventos de boss y especializaciones del SS Carnage. |
| `UI` | HUD, menus, overlays, displays y animacion de botones. |
| `Lore` | Presentacion narrativa por vinetas, seleccion de comics y espera de continuacion. |
| `World` | Entidades del mundo como portales, DealerFish e iluminacion de zona. |
| `Audio` | Adaptadores de musica y mezcla dinamica. |
| `Background` | Parallax y comportamiento de fondo. |
| `MainMenu` | Menu principal y navegacion inicial. |

Regla: un dominio puede consumir contratos de otro dominio, pero no debe apropiarse de su responsabilidad. Por ejemplo, `LevelSpawner` puede instanciar enemigos, pero el algoritmo interno del pez globo vive en `PufferfishEnemy`.

### 2. Orquestadores

Los orquestadores son scripts con autoridad sistemica. Pueden exponer parametros de balance, cablear referencias de escena y decidir transiciones.

Sufijos canonicos:
- `...Controller`: gobierna comportamiento continuo de una entidad o sistema.
- `...Manager`: gobierna un overlay, menu, coordinacion UI o sistema de alto nivel.
- `...Director`: gobierna ritmo macro, eventos de run o coordinacion temporal.
- `...Spawner`: excepcion aceptada cuando el sistema es, semanticamente, un generador runtime.

Sufijos auxiliares permitidos:

| Sufijo | Capa | Uso correcto |
| --- | --- | --- |
| `...State` | Estado formal | Enum o modelo de fase sin dependencias de Unity ni escena. |
| `Runtime...` | Estado runtime | Store estatico o snapshot mutable durante la run. |
| `...Snapshot` | Estado calculado | Lectura inmutable de una situacion compleja. |
| `...Profile` | Datos configurables | Asset o clase serializable de balance/configuracion. No instancia objetos por si mismo. |
| `...Tuning` | Datos de ajuste | Parametros serializables de una especializacion. |
| `...Catalog` | Datos/codigos | IDs, tags, precios base o definiciones estables. |
| `...Repository` | Persistencia | Lectura/escritura de almacenamiento externo. |
| `...Selector` | Servicio interno | Elige una opcion entre datos disponibles, sin tocar escena. |
| `...Resolver` | Servicio interno | Calcula o resuelve un valor derivado de contratos existentes. |
| `...Configurator` | Servicio interno | Aplica configuracion repetible a una instancia creada por un orquestador. |
| `...Presenter` | Presentacion UI | Traduce estado a layout/visual, sin cambiar gameplay. |

Regla de nomenclatura: si un script no necesita ciclo de vida Unity, referencias de Inspector ni `GameObject`, debe preferirse como clase estatica o tipo de datos puro. Si requiere `Update`, coroutines, eventos de escena o referencias serializadas, entonces debe ser `MonoBehaviour` y usar un sufijo de orquestador, especializacion o presenter segun corresponda.

Ejemplos actuales:
- `GameSessionController`
- `RunProgressionDirector`
- `SceneFlowController`
- `CameraController`
- `InkPulseController`
- `PlayerStateController`
- `PlayerVisualStateController`
- `PlayerGadgetInventory`
- `LevelSpawner`
- `BossEventDirector`
- `SSCarnageController`
- `InGameShopManager`
- `PauseMenuManager`
- `GameOverMenuManager`
- `LoreComicPresenter`
- `ZoneLightingController`

`GameUIRoot` no se clasifica como manager porque no gobierna comportamiento. Es un contrato de composicion de escena: agrupa referencias hacia `EventSystem`, HUD, vistas prefab y managers UI para que la jerarquia jugable sea verificable.

`AudioRoot_*`, `CameraRig_*`, `EnviromentRoot_*` y `GameRoot_*` tampoco son managers. Son prefabs de composicion por zona: estabilizan jerarquia, componentes y overrides serializados, pero la autoridad sigue en los controladores y managers que contienen. No deben recibir logica propia ni convertirse en un lugar alternativo para reglas de gameplay.

Reglas:
- Solo los orquestadores o controladores duenos deben exponer parametros ajustables de balance.
- Un orquestador puede depender de estados, catalogos, perfiles y especializaciones.
- Un orquestador no debe duplicar la responsabilidad de otro. Si necesita informacion, debe observar estado/eventos o recibir referencia.
- Las referencias de escena deben cablearse por inspector cuando sean parte del contrato; la resolucion automatica queda como respaldo defensivo.

### 3. Estado formal

El estado formal define fases discretas y observables. Se nombra con `...State` cuando representa una maquina de estado; los snapshots son lecturas inmutables del estado calculado.

Estados actuales:
- `GameSessionState`
- `RunEventState`
- `PlayerRuntimeState`
- `InkPulseState`
- `ShopEventState`
- `SSCarnageAttackState`
- `CameraEventMode`

Estados/runtime stores relacionados:
- `RuntimeInkPulseState`
- `RuntimeGadgetInventory`
- `RuntimeRunScore`
- `RuntimePlayerPace`
- `RunDifficultySnapshot`

Reglas:
- Un `...State` no debe depender de Unity, UI, prefabs ni escena.
- Un estado no debe realizar busquedas de objetos.
- El controlador dueno es quien cambia estado y emite eventos.
- Si una fase afecta a mas de un sistema, debe estar documentada en `StateMachines.md`.
- Si una fase solo afecta un efecto visual local y no bloquea interacciones, puede seguir como ciclo interno no formal.

### 4. Especializaciones

Las especializaciones implementan comportamiento concreto y limitado.

Ejemplos:
- `PufferfishEnemy`
- `FishingRodEnemy`
- `DealerFish`
- `ScenePortal`
- `SSCarnageNetWall`
- `ShrimpCollector`
- `ShrimpValue`
- `GrazeDetector`
- `LightGrazeSource`
- `DestroyOffscreen`
- `HorizontalTracker`
- `ScoreCounterDisplay`
- `ShrimpCounterDisplay`
- `GadgetInventoryHud`
- `MenuButtonAnimation`

Reglas:
- No deben contener parametros globales de progresion o economia.
- No deben decidir rutas de escena, dificultad global, estado de sesion o reglas de tienda.
- Pueden tener datos propios de prefab si esos datos son identidad local: valor del camaron, id de gadget, sprite, collider o referencia visual.
- Si necesitan contexto de spawn, deben recibirlo desde `LevelSpawner` mediante un contrato como `EnemySpawnContext`.
- Si necesitan resolver algo en runtime, debe ser respaldo defensivo y no fuente primaria de arquitectura.

### 5. Datos, catalogos y persistencia

Esta capa concentra datos serializables, identificadores y almacenamiento.

Ejemplos:
- `EnemyTagCatalog`
- `GameplayTagCatalog`
- `GadgetCatalog`
- `GadgetId`
- `GadgetActivationKind`
- `EnemySpawnProfile`
- `PufferfishEnemyTuning`
- `FishingRodEnemyTuning`
- `ZoneSpawnProfile`
- `PortalSpawnPolicy`
- `ShopGadgetOffer`
- `LoreComicEntry`
- `LoreComicEvent`
- `LoreComicZone`
- `UnlockablesCatalogSaveData`
- `PlayerProfileSaveData`
- `PlayerRecordsSaveData`
- `LocalLeaderboardSaveData`
- `PersistentDbPaths`
- `JsonSaveFile`
- `PlayerProfileRepository`
- `PersistentPlayerProfile`
- `LocalLeaderboardRepository`
- `PlayerSkinIds`
- `UnlockablesCatalogQuery`
- `RunGadgetUnlockService`
- `PermanentShopService`
- `PermanentShopPurchaseResult`
- `PermanentUpgradeEffectResolver`
- `BoundaryReferenceDomain`
- `EnemySpawnContext`
- `IEnemySpawnContextReceiver`

Reglas:
- Los catalogos evitan strings locales repetidos.
- Los perfiles/tuning son datos; no ejecutan gameplay.
- La persistencia JSON vive en `PlayerProfileRepository`; gameplay no escribe JSON directamente.
- `Assets/StreamingAssets/db` contiene semillas incluidas en build; `Application.persistentDataPath/db` contiene datos escritos en runtime.
- `player-profile.json` guarda decisiones del jugador: skins, mejoras permanentes y gadgets de run habilitados por hitos.
- `player-records.json` guarda economia y records; `local-leaderboard.json` guarda ranking local.
- `unlockables-catalog.json` separa tres grupos: `skins`, `permanentUpgrades` y `runGadgets`.
- Los gadgets no pertenecen a la tienda out-of-game. Solo se desbloquea su elegibilidad permanente para aparecer en la tienda temporal de la run.
- La tienda out-of-game compra skins y mejoras permanentes mediante `PermanentShopService`.
- Settings de pantalla, volumen, brillo y dificultad no pertenecen a esta base local.

### 6. Servicios internos sin estado Unity

Los servicios internos reducen el tamano de los orquestadores sin quitarles autoridad. No deben ser `MonoBehaviour` si no necesitan `Awake`, `Update`, coroutines ni referencias de Inspector.

Ejemplos actuales:
- `EnemySpawnSelector`: selecciona perfiles de enemigo segun intensidad, peso y regla de cana forzada.
- `SpawnPositionResolver`: calcula posiciones de camaron, enemigo, DealerFish y portal desde camara, boundaries y `ZoneSpawnProfile`.
- `SpawnedObjectConfigurator`: aplica tag, layer, `LightGrazeSource` y `EnemySpawnContext` a objetos recien instanciados por `LevelSpawner`.
- `ShopOfferSelector`: elige una oferta valida de tienda temporal.
- `ShopPriceCalculator`: calcula el precio de oferta desde score, multiplicador global, multiplicador aleatorio y precio base.
- `UnlockablesCatalogQuery`: consulta catalogo persistente, calcula precios por nivel y evalua metas contra records.
- `RunGadgetUnlockService`: convierte metas alcanzadas en gadgets de run habilitados para `InGameShopManager`.
- `PermanentShopService`: valida y ejecuta compras out-of-game de skins y mejoras permanentes.
- `PermanentUpgradeEffectResolver`: expone multiplicadores derivados de niveles persistentes a Ink-Pulse, score y camarones.

Reglas:
- Un `Selector` no instancia ni modifica escena.
- Un `Resolver` no decide si algo debe ocurrir; solo calcula el valor pedido.
- Un `Configurator` puede tocar un `GameObject` ya creado, pero no decide cuando crearlo.
- Un `Calculator` debe ser determinista para los mismos parametros de entrada.
- El orquestador conserva la decision de flujo y el momento de ejecucion.

## Direccion de dependencias

Regla normal:

```text
Controller/Manager/Director
  -> State / Runtime Store
  -> Specialization
  -> Catalog / Data / Repository
```

Regla de eventos:

```text
Specialization -> evento -> Orquestador
```

Ejemplo: `ScenePortal` detecta contacto, pero `SceneFlowController` decide la ruta. `ScenePortal` no es dueno del mapa de escenas.

Regla de UI:

```text
Runtime state -> HUD display
Menu manager -> canvas existente de escena
GameUIRoot -> EventSystem/HUD/vistas/managers UI
```

Los HUD displays observan estado, no lo gobiernan. Los managers de UI no deben autogenerar canvas si la escena ya declara su UI.

Regla de lore:

```text
Sistema de flujo -> LoreComicPresenter -> Comic existente en escena/prefab
```

`MainMenu`, `ScenePortal` y `GameOverMenuManager` pueden solicitar una vineta narrativa, pero no deben seleccionar sprites concretos ni crear UI. Esa seleccion vive en `LoreComicPresenter.entries`, y el arte/layout vive en el prefab o instancia preparada en Unity.

Regla de composicion de escena:

```text
AudioRoot_* prefab -> Soundtrack/SFX
GameRoot_* prefab -> Systems/Player y composicion mayor de zona
CameraRig_* prefab -> Main Camera/CameraController
EnviromentRoot_* prefab -> Background/parallax/luz ambiental de zona
GameUIRoot -> EventSystem/HUD/vistas/managers UI
```

Estos prefabs reducen divergencia estructural entre escenas. Si una referencia externa puede resolverse por contrato estable, como jugador por tag `Player` o boundaries por `BoundaryReferenceResolver`, no debe quedar como requisito fragil del prefab asset.

## Excepciones permitidas

### Resolucion defensiva

Algunos scripts usan `FindFirstObjectByType`, `FindGameObjectWithTag`, `GetComponent` o `TryGetComponent`. Esto es aceptable solo cuando cumple una de estas condiciones:
- recuperar una referencia local del mismo `GameObject`;
- tolerar escenas con referencias incompletas mientras se conserva compatibilidad;
- funcionar como respaldo si el inspector no fue cableado;
- resolver un singleton de infraestructura ya documentado.

No es aceptable usar estas busquedas como mecanismo primario para boundaries, rutas de escena, balance o dificultad.

### Generacion visual local

`ParallaxLayer`, `MenuBubbles` y el fallback de `LightGrazeSource` pueden crear objetos hijos runtime porque generan representacion visual local. No crean managers, canvas de sistema ni reglas de gameplay.

### Herramientas de editor

`Assets/Implementation/Editor/` queda reservado para soporte de build final. Las herramientas historicas de reorganizacion de assets no forman parte de la entrega runtime.

## Resultado de auditoria arquitectonica

### Cumplimientos fuertes

- La sesion global esta centralizada en `GameSessionController`.
- La dificultad macro esta centralizada en `RunProgressionDirector`.
- El spawn de entidades runtime esta centralizado en `LevelSpawner`.
- Los boundaries se resuelven por contrato de jerarquia mediante `BoundaryReferenceResolver`.
- La persistencia permanente esta encapsulada en `PersistentPlayerProfile` y `PlayerProfileRepository`.
- El jugador ya tiene separacion entre movimiento, estado runtime y visuales.
- La tienda in-run no autogenera canvas; usa el canvas de escena.
- `ZonaAbisopelagica` no debe cargar SS Carnage ni `BossNetWall` bajo el contrato actual.

### Riesgos controlados

- `LevelSpawner` conserva la autoridad unica de aparicion runtime, pero ya no concentra seleccion, calculo de posiciones ni configuracion repetible de instancias. Si crece de nuevo, la regla es extraer otro servicio interno antes de crear un segundo spawner.
- `MainMenu` no sigue sufijo `Controller`. Se conserva asi por estabilidad de referencias serializadas de escena; cualquier renombre posterior debe hacerse como refactor controlado.
- `InkPulseMusicCrossfader` es un adaptador de audio, no un controlador de gameplay. Su nombre es valido porque describe una especializacion tecnica.
- `SoundtrackPitchProgression` tambien es un adaptador de audio. Consume `RuntimePlayerPace` como dato de progresion, pero no gobierna dificultad, input, spawn, score ni estado de sesion.
- `LightGrazeSource.EnsureOn()` agrega componentes si la zona tiene iluminacion. Es una excepcion visual deliberada para entidades spawneadas.
- `InGameShopManager` concentra overlay, pausa temporal, botones, oferta activa y transaccion. La seleccion y calculo de precio viven en helpers puros; si el canvas crece, la extension natural es separar una vista/presenter de tienda.
- `OutOfGameShopManager` es el presenter de `ShopMenu`. Coordina seleccion, pagina de skins, interactividad y texto opcional; `PermanentShopService` conserva la autoridad de precios, metas, saldo, limites y persistencia. La aplicacion visual de skins se realiza mediante `PlayerSkinApplier`, fuera de los controladores de gameplay.

### Refactor aplicado

Se extrajeron tipos de estado y datos que estaban incrustados en orquestadores:
- `RunEventState`
- `RunDifficultySnapshot`
- `CameraEventMode`
- `InkPulseState`
- `PlayerRuntimeState`
- `ShopEventState`
- `SSCarnageAttackState`
- `EnemySpawnProfile`
- `PufferfishEnemyTuning`
- `FishingRodEnemyTuning`
- `ZoneSpawnProfile`
- `PortalSpawnPolicy`
- `ShopGadgetOffer`
- `BoundaryReferenceDomain`
- `EnemySpawnContext`
- `IEnemySpawnContextReceiver`
- `GadgetId`
- `GadgetActivationKind`
- `GadgetCatalog`

Tambien se marcaron con `DisallowMultipleComponent` componentes que no deben duplicarse en el mismo nodo:
- `LevelSpawner`
- `CameraController`
- `GrazeDetector`
- `ChargeBar`
- `InkBarFillPresenter`
- `HorizontalTracker`
- `MenuButtonAnimation`

Se extrajeron servicios internos sin estado Unity para bajar responsabilidades de orquestadores:
- `EnemySpawnSelector`
- `SpawnPositionResolver`
- `SpawnedObjectConfigurator`
- `ShopOfferSelector`
- `ShopPriceCalculator`
- `UnlockablesCatalogQuery`
- `RunGadgetUnlockService`
- `PermanentShopService`
- `PermanentUpgradeEffectResolver`

### UI como fachada y presentacion

En UI se permite una separacion adicional para evitar que un display mezcle estado y layout:

- La fachada recibe datos del sistema de gameplay. Ejemplo: `ChargeBar` recibe un valor normalizado desde `InkPulseController`.
- El presenter interpreta ese dato visualmente. Ejemplo: `InkBarFillPresenter` modifica `RectTransform`, mascaras y posicion de efecto.
- La escena o prefab decide que presenter usar. `ZonaEpipelagica` usa `RevealThroughFill`; `ZonaAbisopelagica` usa `FollowFillTip`; `ZonaTutorial` conserva el slider legacy.

Esta separacion permite convertir la UI a prefabs sin duplicar reglas de Ink-Pulse ni acoplar el prefab a la escena.

## Reglas para nuevas implementaciones

- Antes de crear un script nuevo, decidir si es orquestador, estado, especializacion o dato.
- Si el script decide flujo, debe ser `Manager`, `Controller`, `Director` o una excepcion documentada.
- Si el script representa fase, debe ser `...State` y documentarse en `StateMachines.md`.
- Si el script representa comportamiento concreto de prefab, debe ser especializacion y recibir contexto desde el dueno.
- Si el script contiene balance global, no debe vivir en el prefab salvo que el prefab sea el sistema dueno.
- Si una entidad requiere coordinacion entre varias instancias o reglas de aparicion, debe tener manager/controller o ser gobernada por uno existente.
- Si una feature necesita persistencia permanente, debe pasar por repositorio; no escribir archivos desde la entidad.
- Si un sistema necesita boundaries, debe usar la jerarquia `PlayerBoundaries` o `CameraBoundaries`; no agregar campos manuales.

## Criterio para futuras refactorizaciones

Una refactorizacion arquitectonica es necesaria cuando ocurre al menos una de estas senales:
- dos scripts pueden cambiar el mismo estado de gameplay;
- un prefab contiene parametros que deberian balancearse por zona o run;
- un display de UI modifica reglas de juego;
- un enemigo decide dificultad global o rutas de escena;
- un manager crea canvas o nodos que ya pertenecen a la escena;
- un estado formal queda representado por varias banderas no documentadas;
- una zona necesita excepciones manuales para boundaries, camara o spawns.

Si no aparece una de estas senales, se prefiere refactor incremental y documentado antes que cambios masivos de nombres o jerarquias serializadas.
