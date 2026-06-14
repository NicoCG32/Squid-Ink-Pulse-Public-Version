# Flujo de assets

## Runtime en Unity

- `Assets/Content/Audio/Soundtrack/`: musica final para el juego.
- `Assets/Content/Audio/SFX/`: efectos de sonido finales.
- `Assets/Content/Art/Characters/`: sprites/modelos de personajes.
- `Assets/Content/Art/Enemies/`: sprites/modelos de enemigos.
- `Assets/Content/Art/Environments/`: arte de escenarios.
- `Assets/Content/Art/UI/`: recursos visuales de interfaz.
- `Assets/Content/Animations/Characters/`: animaciones de personajes.
- `Assets/Content/Animations/Enemies/`: animaciones de enemigos.
- `Assets/Content/Animations/Environment/`: animaciones de entorno.
- `Assets/Content/Animations/UI/`: animaciones de interfaz.
- `Assets/Content/Prefabs/`: prefabs listos para runtime.

## Prefabs actuales

- `Prefabs/Player/`: `BabySquid`.
- `Prefabs/Enemies/`: `PezGlobo`, `Mina`, `CanaPescar`.
- `Prefabs/Bosses/SSCarnage/`: `SSCarnage`, `BossNetWall`.
- `Prefabs/Gadgets/`: `ShellShield`, `InkBottle`.
- `Prefabs/Shop/`: `DealerFish`.
- `Prefabs/Portals/`: `ScenePortal`.
- `Prefabs/Collectibles/`: camarones normales y x10.
- `Prefabs/World/`: `CleanUp`.
- `Prefabs/UI/HUD/`: barras Ink-Pulse y piezas HUD reutilizables.
- `Prefabs/UI/Menus/`: vistas de pausa, game over y tienda in-run.

## Regla para prefabs

- El prefab define identidad visual, collider propio, capa/tag esperado y script de comportamiento propio.
- El prefab no debe guardar referencias a objetos de escena como jugador, camara o boundaries.
- Si necesita jugador o camara, el manager o el script los resuelve en runtime.
- Si necesita limites, usa `BoundaryReferenceResolver`.
- Si es gadget comprable, usa `GadgetShopItem`; no debe actuar como pickup directo.
- Si es prefab UI, no debe guardar referencias a managers, jugador o escena; esas referencias las asigna la escena o el controlador que consume la vista.

## UI/HUD

Las barras Ink-Pulse se separan en tres prefabs para conservar variantes por zona sin mezclar responsabilidades:

- `Assets/Content/Prefabs/UI/HUD/InkBarHorizontal.prefab`: `ZonaEpipelagica`, barra horizontal/rotada, con `InkBarFillPresenter` en modo `RevealThroughFill`.
- `Assets/Content/Prefabs/UI/HUD/InkBarVertical.prefab`: `ZonaAbisopelagica`, barra vertical, con `InkBarFillPresenter` en modo `FollowFillTip`.
- `Assets/Content/Prefabs/UI/HUD/InkPulseBarLegacy.prefab`: `ZonaTutorial`, barra legacy con `Slider`.

La escena puede conservar overrides de posicion, rotacion, escala y referencias hacia managers/controladores. La jerarquia interna, mascara, animador y componentes de presentacion deben mantenerse en el prefab. En `ZonaEpipelagica` y `ZonaAbisopelagica`, estas piezas deben existir como instancias prefab.

Vistas de menu disponibles:

- `Assets/Content/Prefabs/UI/Menus/PauseMenu.prefab`: vista `PauseCanvas`, sin referencias persistentes a `PauseMenuManager`.
- `Assets/Content/Prefabs/UI/Menus/GameOverMenu.prefab`: vista `GameOverCanvas`, sin referencias persistentes a `GameOverMenuManager`.
- `Assets/Content/Prefabs/UI/Menus/InGameShopMenu.prefab`: vista `InGameCanvas`, sin referencias persistentes a `InGameShopManager`.

Piezas HUD disponibles:

- `Assets/Content/Prefabs/UI/HUD/GadgetSlots.prefab`
- `Assets/Content/Prefabs/UI/HUD/ShrimpCounter.prefab`
- `Assets/Content/Prefabs/UI/HUD/ScoreCounter.prefab`

Regla de eventos:
- Los prefabs de vista no deben guardar `onClick` persistentes hacia managers de escena.
- `PauseMenuManager`, `GameOverMenuManager` e `InGameShopManager` cablean listeners en runtime.
- La migracion/validacion de estas instancias vive en `Assets/Implementation/Editor/GameplayUiPrefabSceneMigration.cs`.

## World prefabs

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
- `GrazeZone`;
- `SquidVisual` para movimiento base;
- `InkPulseVisual` para el impulso largo de tinta.

Reglas:
- `ZonaEpipelagica`, `ZonaAbisopelagica` y `ZonaTutorial` deben usar instancias del prefab, aunque el nodo de escena se llame `Squid`.
- El prefab base solo guarda referencias internas; sesion, camara, HUD, progression director y boundaries no se serializan dentro del asset.
- Las instancias `Squid` de cada escena si deben tener esas referencias externas asignadas desde Inspector.
- La resolucion runtime existe como respaldo, no como sustituto del cableado de escena.
- Las capacidades especificas de zona, como `LightGrazeSource` en `ZonaAbisopelagica`, deben ser overrides de instancia o agregarse por managers de zona, no incorporarse al prefab base.
- Las skins deben cambiar visuales o variants, no duplicar controladores de gameplay.

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
