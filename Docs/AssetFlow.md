# Flujo de assets

## Runtime en Unity

- `Assets/Content/Audio/Soundtrack/`: musica final para el juego.
- `Assets/Content/Audio/SFX/`: efectos de sonido finales.
- `Assets/Content/Art/Characters/`: sprites/modelos de personajes.
- `Assets/Content/Art/Enemies/`: sprites/modelos de enemigos.
- `Assets/Content/Art/Environments/`: arte de escenarios.
- `Assets/Content/Art/Environments/ShopMenu/Fondo.png`: fondo de escena de la tienda global; no pertenece al paquete de controles UI.
- `Assets/Content/Art/UI/`: recursos visuales de interfaz.
- `Assets/Content/Art/UI/ShopMenu/Resources/ShopMenu/`: sprites cargados por `OutOfGameShopManager` para mejoras y skins; las rutas del catalogo parten bajo `ShopMenu/`.
- `Assets/Content/Art/ComicLore/`: vinetas narrativas organizadas por dominio (`Inicio`, `Portales`, `Derrota/Epipelagica`, `Derrota/Abisopelagica`, `Tienda`).
- `Assets/Content/Animations/Characters/`: animaciones de personajes.
- `Assets/Content/Animations/Characters/BabySquid/default/`: fuentes de movimiento, Ink-Pulse y portal de la skin base.
- `Assets/Content/Animations/Characters/BabySquid/<Skin>/`: fuentes y salidas generadas de cada skin jugable implementada.
- `Assets/Content/Animations/Enemies/`: animaciones de enemigos.
- `Assets/Content/Animations/Environment/`: animaciones de entorno.
- `Assets/Content/Animations/UI/`: animaciones de interfaz.
- `Assets/Content/Prefabs/`: prefabs listos para runtime.
- `Assets/Implementation/Config/Spawning/`: perfiles `ZoneSpawnProfile` por zona.

## Configuracion de spawn por zona

Los parametros de spawn ya no viven como fallback dentro de `LevelSpawner`. Cada zona jugable debe asignar un asset `ZoneSpawnProfile`:

- `Assets/Implementation/Config/Spawning/ZonaEpipelagicaSpawnProfile.asset`
- `Assets/Implementation/Config/Spawning/ZonaAbisopelagicaSpawnProfile.asset`
- `Assets/Implementation/Config/Spawning/ZonaTutorialSpawnProfile.asset`

Regla:
- `LevelSpawner` orquesta la aparicion runtime.
- `ZoneSpawnProfile` almacena prefabs, pesos, intervalos, tienda, portales y tuning de enemigos.
- Una zona jugable con `zoneSpawnProfile` vacio esta mal configurada.
- `Tools/Squid/Validate Scene Contracts` valida que cada zona apunte a su perfil correcto.

## Prefabs actuales

- `Prefabs/Player/`: `BabySquid`.
- `Prefabs/Enemies/`: `PezGlobo`, `Mina`, `CanaPescar`, `Ray`, `Jellyfish`.
- `Prefabs/Bosses/SSCarnage/`: `SSCarnage`, `BossNetWall`.
- `Prefabs/Bosses/UnknownBoss/`: `UnknownBoss`, `BossPillars`.
- `Prefabs/Gadgets/`: `ShellShield`, `InkBottle`.
- `Prefabs/Shop/`: `DealerFish` y `DealerFish_ZonaAbisopelagica`.
- `Prefabs/Portals/`: `ScenePortal`.

Los prefabs de gadgets representan mercancia de run. Su disponibilidad permanente se define por id en `Assets/StreamingAssets/db/unlockables-catalog.json/runGadgets`; su posesion durante una partida vive en `RuntimeGadgetInventory`.
- `Prefabs/Collectibles/`: camarones normales y x10.
- `Prefabs/Core/Audio/`: `AudioRoot_*` por zona jugable.
- `Prefabs/Core/Camera/`: `CameraRig_*` por zona jugable.
- `Prefabs/Core/Environment/`: `EnviromentRoot_*` por zona jugable.
- `Prefabs/Core/Scenes/`: `GameRoot_*` por zona jugable.
- `Prefabs/World/`: `Boundaries`, `CleanUp`.
- `Prefabs/UI/HUD/`: barras Ink-Pulse y piezas HUD reutilizables.
- `Prefabs/UI/Menus/`: vistas de pausa, game over, tienda in-run, `OptionsMenu` global y overlay `LoreComic`.

## Regla para prefabs

- El prefab define identidad visual, collider propio, capa/tag esperado y script de comportamiento propio.
- El prefab no debe guardar referencias a objetos de escena como jugador, camara o boundaries.
- Si necesita jugador o camara, el manager o el script los resuelve en runtime.
- Si necesita limites, usa `BoundaryReferenceResolver`.
- Si es gadget comprable, usa `GadgetShopItem`; no debe actuar como pickup directo.
- Si es contenido permanente, debe tener id estable en `unlockables-catalog.json`; no guardar referencias Unity directas dentro del JSON.
- Si es prefab UI, no debe guardar referencias a managers, jugador o escena; esas referencias las asigna la escena o el controlador que consume la vista.

## Core scene prefabs

`Assets/Content/Prefabs/Core/Audio/` contiene una instancia prefab de `Audio` por zona:

- `AudioRoot_ZonaEpipelagica.prefab`
- `AudioRoot_ZonaAbisopelagica.prefab`
- `AudioRoot_ZonaTutorial.prefab`

`AudioRoot_*` agrupa `Soundtrack` y `SFX`. En `ZonaEpipelagica`, `Soundtrack` incluye dos `AudioSource` y `InkPulseMusicCrossfader`; en `ZonaAbisopelagica` conserva una pista base. `ZonaEpipelagica` y `ZonaAbisopelagica` usan `SoundtrackPitchProgression` para subir pitch segun la progresion efectiva de la run. El prefab no debe depender de referencias externas: `InkPulseMusicCrossfader` resuelve el `InkPulseController` en runtime si no esta serializado, y `SoundtrackPitchProgression` lee `RuntimePlayerPace`.

`Assets/Content/Prefabs/Core/Camera/` contiene una instancia prefab de `CameraRig` por zona:

- `CameraRig_ZonaEpipelagica.prefab`
- `CameraRig_ZonaAbisopelagica.prefab`
- `CameraRig_ZonaTutorial.prefab`

`CameraRig` agrupa `Main Camera`, `AudioListener`, `Camera` y `CameraController`. Las zonas pueden conservar overrides de posicion, ortografia y parametros de camara. `CameraController` resuelve al jugador por tag `Player` si `target` no esta serializado, por lo que el prefab no depende de una referencia externa obligatoria al squid.

`Assets/Content/Prefabs/Core/Environment/` contiene una instancia prefab de `Enviroment` por zona:

- `EnviromentRoot_ZonaEpipelagica.prefab`
- `EnviromentRoot_ZonaAbisopelagica.prefab`
- `EnviromentRoot_ZonaTutorial.prefab`

`EnviromentRoot_*` agrupa fondos, capas de parallax, luz global y efectos visuales de zona. `ParallaxLayer` resuelve `Camera.main` como respaldo si `cameraTransform` no esta serializado, por lo que el prefab puede aplicarse sin depender de enlaces fragiles a una camara de escena.

`EnviromentRoot_ZonaAbisopelagica` contiene cinco capas de parallax (`Layer1` a `Layer5`) y cada capa debe conservar sus parametros de reciclaje/culling serializados. `Layer5` pertenece al contrato visual actual de la zona y no debe tratarse como legacy.

`Assets/Content/Prefabs/Core/Scenes/` contiene una instancia prefab de `GameRoot` por zona:

- `GameRoot_ZonaEpipelagica.prefab`
- `GameRoot_ZonaAbisopelagica.prefab`
- `GameRoot_ZonaTutorial.prefab`

Estos prefabs son raices de composicion por zona, no prefabs globales compartidos. Conservan la estructura mayor de `GameRoot`, `Systems` y `Player`, junto a los overrides propios de cada escena. Si en el futuro las tres zonas estabilizan una jerarquia identica, se puede extraer un prefab base comun y dejar estas piezas como variants.

`GameRoot_*` tambien conserva el subroot `Gameplay`, que agrupa `LevelSpawner`, `Boundaries`, `CleanUp`, `Bosses` y `Portals`. No se separo `Gameplay` como prefab independiente porque Unity no permite guardar directamente una parte de una instancia prefab como nuevo prefab sin reestructurar el asset padre. Arquitectonicamente, ese subroot ya queda protegido por el contrato de `GameRoot_*` y por `Tools/Squid/Validate Scene Contracts`.

## UI/HUD

La barra Ink-Pulse tiene una sola fuente canonica: `Assets/Content/Prefabs/UI/HUD/InkBar.prefab`. El prefab conserva `ChargeBar` e `InkBarFillPresenter`; la orientacion visual se autoriza en el asset, no mediante variantes por zona.

La escena puede conservar overrides de posicion y referencias hacia managers/controladores. La jerarquia interna, mascara, animador y componentes de presentacion deben mantenerse en el prefab. `FillViewport` usa una mascara visible con grafico blanco translucido; si se apaga `Show Mask Graphic`, se oculta ese fondo. En `ZonaEpipelagica`, `ZonaAbisopelagica` y `ZonaTutorial`, `GameRoot/GameUIRoot/HUD/InkBar` debe existir como instancia de ese unico prefab.

Vistas de menu disponibles:

- `Assets/Content/Prefabs/UI/Menus/PauseMenu.prefab`: vista `PauseCanvas`, sin referencias persistentes a `PauseMenuManager`.
- `Assets/Content/Prefabs/UI/Menus/GameOverMenu.prefab`: vista `GameOverCanvas`, sin referencias persistentes a `GameOverMenuManager`.
- `Assets/Content/Prefabs/UI/Menus/InGameShopMenu.prefab`: vista `InGameCanvas`, sin referencias persistentes a `InGameShopManager`.
- `Assets/Content/Prefabs/UI/Menus/LoreComic.prefab`: vista narrativa `LoreComicRoot/Comic`, con referencias internas a `LoreComicPresenter`.
- `Assets/Content/Prefabs/UI/Menus/OptionsMenu.prefab`: panel global de pantalla/volumen, instalado como root de escena separado para preservar su escala de Canvas.

`ShopMenu` es una escena, no un prefab de tienda. El arte de sus vitrinas y decoraciones vive bajo `Panel`; los controles transparentes serializados viven bajo `Panel/ShopInteractionRoot` y apuntan desde Inspector a `OutOfGameShopManager`. La presentacion funcional del producto seleccionado vive en `Panel/ProductInfoBlock` con `NombreProducto`, `DescripcionProducto` y `PrecioProducto`; el manager escribe contenido, mientras que la posicion y el estilo de esos nodos se editan manualmente en Unity.

Los sprites de productos permanentes no viven en carpetas temporales. Deben importarse bajo `Assets/Content/Art/UI/ShopMenu/Resources/ShopMenu/`:
- Mejoras: `Skills/Upgrades/Nombre` y, para seleccion, `Skills/Upgrades/NombreInk`.
- Skins: `Skins/Nombre`.
- Estados de skins compradas/equipadas usan, cuando existan, las rutas `shopBuyedSpriteResourcePath` y `shopSelectedSpriteResourcePath` del catalogo. Si faltan, la tienda conserva fallback hacia el sprite base sin cambiar el arte por codigo.

`OutOfGameShopManager` no mueve assets ni crea sprites en runtime. Solo carga rutas de `Resources` declaradas en `unlockables-catalog.json`.

Una skin aparece en el catalogo runtime solo cuando tiene prefab visual bajo `Assets/Content/Prefabs/Player/Resources/PlayerSkins/` y carpeta de animaciones jugables bajo `Assets/Content/Animations/Characters/BabySquid/<Skin>/`. Las skins preparadas como concepto pero sin prefab/animacion quedan en fuentes de diseno, no en `unlockables-catalog.json`.

Piezas HUD disponibles:

- `Assets/Content/Prefabs/UI/HUD/GadgetSlots.prefab`
- `Assets/Content/Prefabs/UI/HUD/ShrimpCounter.prefab`
- `Assets/Content/Prefabs/UI/HUD/ScoreCounter.prefab`

Regla de eventos:
- Los prefabs de vista no deben guardar `onClick` persistentes hacia managers externos de escena.
- Si el listener apunta a un componente del mismo prefab, el `onClick` persistente es valido porque queda autocontenido y auditable.
- `LoreComic.prefab` mantiene el listener persistente de su propio `ContinuarBoton` hacia `LoreComicPresenter.Continue()`, porque ambos viven dentro del mismo prefab.
- `PauseMenuManager`, `GameOverMenuManager` e `InGameShopManager` pueden conservar cableado runtime como respaldo defensivo durante migracion, pero el contrato preferido es referencia visible/serializada y no dependencia oculta por busqueda.
- La migracion/validacion de estas instancias vive en `Assets/Implementation/Editor/GameplayUiPrefabSceneMigration.cs`.

## World prefabs

`Assets/Content/Prefabs/World/Boundaries.prefab` es la fuente canonica de la jerarquia de limites de gameplay.

Jerarquia:

```text
Boundaries
|-- CameraBoundaries
|   |-- TopBoundary
|   `-- BottomBoundary
`-- PlayerBoundaries
    |-- TopBoundary
    `-- BottomBoundary
```

Reglas:
- `Boundaries` debe existir como instancia prefab bajo `GameRoot/Gameplay` en cada zona jugable.
- La instancia puede conservar overrides de posicion y tamano de colliders por zona.
- La estructura, nombres obligatorios e `HorizontalTracker` pertenecen al prefab.
- El prefab no guarda referencias de escena; `HorizontalTracker` resuelve `Camera.main` en runtime.
- No se deben crear copias locales desempaquetadas para ajustar una zona. Si cambia la altura jugable, se ajustan los colliders de la instancia prefab.

`Assets/Content/Prefabs/World/CleanUp.prefab` es la fuente canonica de limpieza fuera de camara.

Jerarquia:

```text
CleanUp
`-- DestroyZone
    `-- GarbageCollector
```

Reglas:
- `CleanUp` debe existir como instancia prefab bajo `GameRoot/Gameplay` en cada zona jugable.
- `GarbageCollector` contiene `DestroyOffscreen`, `BoxCollider2D` trigger y `Rigidbody2D` kinematic.
- El prefab no guarda referencias de escena; `DestroyOffscreen` resuelve `Camera.main` y `CameraBoundaries` en runtime.
- El alto del trigger no se escala ni se balancea a mano: se calcula desde la distancia interna entre `CameraBoundaries/BottomBoundary` y `CameraBoundaries/TopBoundary`.
- Si una escena cambia dimensiones, se ajustan los colliders de `CameraBoundaries`; el prefab se adapta automaticamente.

Contrato de escena:
- Las escenas jugables usan un root llamado `GameUIRoot`.
- `GameUIRoot` tiene `Assets/Implementation/Code/UI/GameUIRoot.cs` y conserva referencias a `EventSystem`, `HUD`, vistas prefab y managers UI.
- `GameUIRoot` no instancia prefabs, no navega escenas y no decide estados de pausa, tienda o derrota.
- Si se cambia la composicion de UI, se debe actualizar `GameUIRoot` y validar con `Tools/Squid/Validate Gameplay UI Prefab Instances`.

## Player prefab

`Assets/Content/Prefabs/Player/BabySquid.prefab` es la fuente canonica del jugador.

Incluye:
- root `BabySquid` con tag `Player` y layer `Player`;
- collider y rigidbody de gameplay;
- scripts de movimiento, Ink-Pulse, colision, camarones, gadgets y estado runtime;
- `SkinMount` como punto de montaje de skins visuales cargadas desde `Resources`;
- `GrazeZone`;
- `SquidVisual` para movimiento base;
- `InkPulseVisual` para el impulso largo de tinta;
- `PortalVisual` para la transicion visual de portal.

Reglas:
- `ZonaEpipelagica`, `ZonaAbisopelagica` y `ZonaTutorial` deben usar instancias del prefab, aunque el nodo de escena se llame `Squid`.
- El prefab base solo guarda referencias internas; sesion, camara, HUD, progression director y boundaries no se serializan dentro del asset.
- Las instancias `Squid` de cada escena si deben tener esas referencias externas asignadas desde Inspector.
- La resolucion runtime existe como respaldo, no como sustituto del cableado de escena.
- Las capacidades especificas de zona, como `LightGrazeSource` en `ZonaAbisopelagica`, deben ser overrides de instancia o agregarse por managers de zona, no incorporarse al prefab base.
- Las skins deben cambiar visuales o variants, no duplicar controladores de gameplay.
- Un prefab de skin debe vivir bajo alguna carpeta `Resources` y exponerse en el catalogo con `playerSkinPrefabResourcePath` sin extension.
- El prefab de skin debe contener `MovementVisual` o `SquidVisual`, `InkPulseVisual` y `PortalVisual`; cada raiz puede tener su propio `Animator`.
- Los prefabs de skin no deben incluir `Rigidbody2D`, colliders de gameplay ni scripts del jugador canonico.

## Runtime en UI MainMenu

- `Assets/Content/Animations/UI/MainMenu/Character/`
- `Assets/Content/Animations/UI/MainMenu/Background/`
- `Assets/Content/Animations/UI/MainMenu/Buttons/`
- `Assets/Content/Art/UI/MainMenu/Character/`
- `Assets/Content/Art/UI/MainMenu/Background/`
- `Assets/Content/Art/UI/MainMenu/Buttons/`
- `Assets/Content/Audio/UI/MainMenu/Character/`
- `Assets/Content/Audio/UI/MainMenu/Background/`
- `Assets/Content/Audio/UI/MainMenu/Buttons/`
- `Assets/Implementation/Code/MainMenu/`

## Flujo recomendado

1. Ubicar arte, audio y animaciones en la carpeta funcional correspondiente.
2. Integrar animaciones en prefabs o elementos de UI segun dominio.
3. Mantener colliders de gameplay en objetos claros y documentados.
4. Vincular logica desde scripts del dominio correspondiente.
5. Validar operacion en escena runtime.
6. Confirmar que no quedaron referencias de escena serializadas dentro del prefab.

## Soundtrack dinamico

Las versiones normal e intensa de una misma musica deben exportarse con:

- mismo punto inicial;
- mismo tempo;
- misma duracion o loop perfectamente equivalente;
- misma afinacion.

En `ZonaEpipelagica`, el nodo `Soundtrack` mantiene dos `AudioSource`: normal e `INK`. `InkPulseMusicCrossfader` las inicia sincronizadas y cruza volumen segun `InkPulseState.Active`.

Regla de mezcla:
- Si la pista `INK` es una mezcla completa alternativa, usar crossfade lineal complementario.
- Si en el futuro se usan stems complementarios que no duplican el mismo contenido, puede probarse `useEqualPowerCrossfade`.

Regla de pitch progresivo:
- `SoundtrackPitchProgression` no reemplaza al crossfade; solo suma un offset de pitch sobre el pitch base de cada `AudioSource`.
- La formula runtime es `pitchBase + min(maxPitchOffset, RuntimePlayerPace.ElapsedSpeedSeconds * pitchIncreasePerSecond)`.
- Los valores actuales son `pitchIncreasePerSecond = 0.0005` y `maxPitchOffset = 0.18`.
- Si el pitch base del `AudioSource` es `1.1`, el maximo efectivo queda en `1.28`.
- En `ZonaEpipelagica`, ambas pistas deben recibir el mismo offset para evitar que el crossfade revele desajuste musical.
