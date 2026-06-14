# Arquitectura de software

## Proposito

Este documento formaliza la arquitectura runtime de Squid Ink-Pulse. Su objetivo es que cada sistema tenga un propietario claro, que las maquinas de estado sean visibles, y que el crecimiento hacia tienda permanente, tutorial, skins, zonas y enemigos no produzca scripts redundantes o contradictorios.

El informe historico en `Docs/Reports/` no se reescribe. Este documento describe el contrato vivo del codigo actual.

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
- `ZoneLightingController`

`GameUIRoot` no se clasifica como manager porque no gobierna comportamiento. Es un contrato de composicion de escena: agrupa referencias hacia `EventSystem`, HUD, vistas prefab y managers UI para que la jerarquia jugable sea verificable.

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
- `PlayerProfileSaveData`
- `PlayerProfileRepository`
- `PersistentPlayerProfile`
- `PlayerSkinIds`
- `BoundaryReferenceDomain`
- `EnemySpawnContext`
- `IEnemySpawnContextReceiver`

Reglas:
- Los catalogos evitan strings locales repetidos.
- Los perfiles/tuning son datos; no ejecutan gameplay.
- La persistencia JSON vive en `PlayerProfileRepository`; gameplay no escribe JSON directamente.
- Settings de pantalla, volumen, brillo y dificultad no pertenecen a `player-profile.json`.

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

## Excepciones permitidas

### Resolucion defensiva

Algunos scripts usan `FindFirstObjectByType`, `FindGameObjectWithTag`, `GetComponent` o `TryGetComponent`. Esto es aceptable solo cuando cumple una de estas condiciones:
- recuperar una referencia local del mismo `GameObject`;
- tolerar escenas antiguas durante migracion;
- funcionar como respaldo si el inspector no fue cableado;
- resolver un singleton de infraestructura ya documentado.

No es aceptable usar estas busquedas como mecanismo primario para boundaries, rutas de escena, balance o dificultad.

### Generacion visual local

`ParallaxLayer`, `MenuBubbles` y el fallback de `LightGrazeSource` pueden crear objetos hijos runtime porque generan representacion visual local. No crean managers, canvas de sistema ni reglas de gameplay.

### Herramientas de editor

`Assets/Implementation/Editor/` puede usar migraciones amplias, `AddComponent` y normalizacion de escenas. No forma parte de la arquitectura runtime.

## Hallazgos de la auditoria actual

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

- `LevelSpawner` sigue siendo un archivo grande porque concentra spawn de camarones, enemigos, DealerFish y portales. Es aceptable por ahora porque es el dueno unico de aparicion runtime. Si crece mas, el siguiente paso es separar estrategias internas sin mover la autoridad fuera del spawner.
- `MainMenu` no sigue sufijo `Controller`. No se renombro para evitar perder referencias serializadas de escena. Refactor futuro recomendado: `MainMenuController` con migracion de escena.
- `InkPulseMusicCrossfader` es un adaptador de audio, no un controlador de gameplay. Su nombre es valido porque describe una especializacion tecnica.
- `LightGrazeSource.EnsureOn()` agrega componentes si la zona tiene iluminacion. Es una excepcion visual deliberada para entidades spawneadas.

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
