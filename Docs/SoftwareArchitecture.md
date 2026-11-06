# Arquitectura de software

## Proposito

La arquitectura runtime de Squid Ink-Pulse asigna un propietario a cada sistema. Los controladores coordinan el flujo; los estados describen fases; los servicios, perfiles y adaptadores separan las reglas de su presentación en Unity.

## Tesis arquitectonica

La base del proyecto se organiza por dominios. Dentro de cada dominio se respeta esta direccion conceptual:

```text
Dominio
`-- Orquestadores: ...Manager, ...Controller, ...Director o spawner de sistema
    `-- Estado formal: ...State, runtime state stores o snapshots de lectura
        `-- Especializaciones: entidades, efectos, displays, fuentes, valores y adaptadores
            `-- Datos locales: catalogos, perfiles, tuning, repositorios y definiciones
```

El estado formal modela la fase del sistema. El controlador o manager ejecuta la transición y aplica sus efectos sobre los componentes. Muchos `...State` son enumeraciones puras.

## Capas

### 1. Dominios

Un dominio es una carpeta bajo `Assets/Implementation/Code/` con una responsabilidad funcional principal.

| Dominio | Responsabilidad |
| --- | --- |
| `Core` | Sesión, escenas, progresión, cámara, boundaries e infraestructura transversal. |
| `Player` | Movimiento, Ink-Pulse, colisiones, inventario, visuales y perfil persistente. |
| `Spawning` | Aparicion de enemigos, camarones, tienda in-run y portales. |
| `Enemies` | Comportamiento propio de enemigos concretos. |
| `Bosses` | Orquestacion de eventos de boss y especializaciones del SS Carnage. |
| `UI` | HUD, menus, overlays, displays y animación de botones. |
| `Lore` | Presentacion narrativa por viñetas, seleccion de comics y espera de continuacion. |
| `World` | Entidades del mundo como portales, DealerFish e iluminacion de zona. |
| `Audio` | Adaptadores de musica y mezcla dinamica. |
| `Background` | Parallax y comportamiento de fondo. |
| `MainMenu` | Menú principal y navegación inicial. |

Regla: un dominio puede consumir contratos de otro dominio, pero no debe apropiarse de su responsabilidad. Por ejemplo, `LevelSpawner` puede instanciar enemigos, pero el algoritmo interno del pez globo vive en `PufferfishEnemy`.

### 2. Orquestadores

Los orquestadores son scripts con autoridad sistemica. Pueden exponer parámetros de balance, cablear referencias de escena y decidir transiciones.

Sufijos canonicos:
- `...Controller`: gobierna comportamiento continuo de una entidad o sistema.
- `...Manager`: gobierna un overlay, menú, coordinacion UI o sistema de alto nivel.
- `...Director`: gobierna ritmo macro, eventos de run o coordinacion temporal.
- `...Spawner`: excepción aceptada cuando el sistema es, semánticamente, un generador runtime.

Sufijos auxiliares permitidos:

| Sufijo | Capa | Uso correcto |
| --- | --- | --- |
| `...State` | Estado formal | Enum o modelo de fase sin dependencias de Unity ni escena. |
| `Runtime...` | Estado runtime | Store estático o snapshot mutable durante la run. |
| `...Snapshot` | Estado calculado | Lectura inmutable de una situacion compleja. |
| `...Profile` | Datos configurables | Asset o clase serializable de balance/configuración. No instancia objetos por sí mismo. |
| `...Tuning` | Datos de ajuste | Parámetros serializables de una especializacion. |
| `...Catalog` | Datos/codigos | IDs, tags, precios base o definiciones estables. |
| `...Repository` | Persistencia | Lectura/escritura de almacenamiento externo. |
| `...Selector` | Servicio interno | Elige una opcion entre datos disponibles, sin tocar escena. |
| `...Resolver` | Servicio interno | Calcula o resuelve un valor derivado de contratos existentes. |
| `...Configurator` | Servicio interno | Aplica configuración repetible a una instancia creada por un orquestador. |
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

`GameUIRoot` no se clasifica como manager porque no gobierna comportamiento. Es un contrato de composicion de escena: agrupa referencias hacia `EventSystem`, HUD, vistas prefab y managers UI para que la jerarquía jugable sea verificable.

`AudioRoot_*`, `CameraRig_*`, `EnviromentRoot_*` y `GameRoot_*` tampoco son managers. Son prefabs de composicion por zona: estabilizan jerarquía, componentes y overrides serializados, pero la autoridad sigue en los controladores y managers que contienen. No deben recibir lógica propia ni convertirse en un lugar alternativo para reglas de gameplay.

Reglas:
- Solo los orquestadores o controladores dueños deben exponer parámetros ajustables de balance.
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
- No deben contener parámetros globales de progresión o economia.
- No deben decidir rutas de escena, dificultad global, estado de sesión o reglas de tienda.
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
- `Assets/Implementation/Resources/PersistentDbSeeds` contiene la fuente única de semillas; `IJsonSeedProvider` abstrae su lectura y `Application.persistentDataPath/db` contiene datos escritos en runtime.
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
- `SpawnPositionResolver`: calcula posiciones de camaron, enemigo, DealerFish y portal desde cámara, boundaries y `ZoneSpawnProfile`.
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
- Un `Calculator` debe ser determinista para los mismos parámetros de entrada.
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
Menú manager -> canvas existente de escena
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

`Assets/Implementation/Editor/` contiene builders, validadores y exportadores de previsualización. Estas herramientas quedan fuera del player distribuido.

## Distribución de responsabilidades

La sesión global pertenece a `GameSessionController`, la progresión a `RunProgressionDirector` y la aparición de entidades a `LevelSpawner`. El spawner delega selección, posición y configuración en `EnemySpawnSelector`, `SpawnPositionResolver` y `SpawnedObjectConfigurator`.

`PersistentPlayerProfile` y `PlayerProfileRepository` encapsulan el progreso permanente. Las tiendas delegan precios y transacciones en servicios de dominio; `OutOfGameShopManager` presenta `ShopMenu`, mientras `PlayerSkinApplier` aplica las skins al personaje.

`MainMenu` conserva sus métodos serializados y delega navegación y código secreto en `MainMenuSceneNavigator` y `MainMenuSecretCodeProgress`. `LoreComicPresenter` coordina pausa y corrutinas, con selección, persistencia y vista separadas.

Los adaptadores de audio y luz observan el estado del juego. `InkPulseMusicCrossfader` mezcla pistas, `SoundtrackPitchProgression` ajusta pitch y `LightGrazeSource` declara fuentes visuales para las zonas iluminadas.

### UI como fachada y presentación

En UI se permite una separacion adicional para evitar que un display mezcle estado y layout:

- La fachada recibe datos del sistema de gameplay. Ejemplo: `ChargeBar` recibe un valor normalizado desde `InkPulseController`.
- El presenter interpreta ese dato visualmente. Ejemplo: `InkBarFillPresenter` modifica `RectTransform`, mascaras y posición de efecto.
- La escena o prefab decide que presenter usar. `ZonaEpipelagica` usa `RevealThroughFill` y `ZonaAbisopelagica` usa `FollowFillTip`.

Esta separacion permite convertir la UI a prefabs sin duplicar reglas de Ink-Pulse ni acoplar el prefab a la escena.

## Reglas para nuevas implementaciones

- Antes de crear un script nuevo, decidir si es orquestador, estado, especializacion o dato.
- Si el script decide flujo, debe ser `Manager`, `Controller`, `Director` o una excepción documentada.
- Si el script representa fase, debe ser `...State` y documentarse en `StateMachines.md`.
- Si el script representa comportamiento concreto de prefab, debe ser especializacion y recibir contexto desde el dueno.
- Si el script contiene balance global, no debe vivir en el prefab salvo que el prefab sea el sistema dueno.
- Si una entidad requiere coordinacion entre varias instancias o reglas de aparición, debe tener manager/controller o ser gobernada por uno existente.
- Si una feature necesita persistencia permanente, debe pasar por repositorio; no escribir archivos desde la entidad.
- Si un sistema necesita boundaries, debe usar la jerarquía `PlayerBoundaries` o `CameraBoundaries`; no agregar campos manuales.

## Criterio para futuras refactorizaciones

Una refactorizacion arquitectonica es necesaria cuando ocurre al menos una de estas senales:
- dos scripts pueden cambiar el mismo estado de gameplay;
- un prefab contiene parámetros que deberian balancearse por zona o run;
- un display de UI modifica reglas de juego;
- un enemigo decide dificultad global o rutas de escena;
- un manager crea canvas o nodos que ya pertenecen a la escena;
- un estado formal queda representado por varias banderas no documentadas;
- una zona necesita excepciones manuales para boundaries, cámara o spawns.

Si no aparece una de estas senales, se prefiere refactor incremental y documentado antes que cambios masivos de nombres o jerarquias serializadas.
