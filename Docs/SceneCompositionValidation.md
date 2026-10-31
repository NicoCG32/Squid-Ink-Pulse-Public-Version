# Scene Composition Validation

## Alcance

`SceneCompositionValidator` valida contratos criticos de composicion sin cambiar comportamiento jugable ni balance.

Se ejecuta manualmente desde:

```text
Tools/Squid Ink Pulse/Validate Scene Composition
```

Y en batch:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.com' `
  -batchmode `
  -nographics `
  -quit `
  -projectPath "$PWD" `
  -executeMethod SceneCompositionValidator.ValidateSceneComposition `
  -logFile "$PWD\TestResults\scene-composition-validation.log"
```

Tambien se ejecuta como `IPreprocessBuildWithReport`, antes de compilar un build.

## Contratos validados

- `EditorBuildSettings` contiene exactamente `MainMenu`, `ZonaEpipelagica`, `ZonaAbisopelagica` y `ShopMenu`.
- Los prefabs canonicos principales no tienen scripts faltantes.
- Las cuatro escenas de build no tienen scripts faltantes.
- `MainMenu` y `ShopMenu` tienen un unico `EventSystem` y su manager principal.
- `MainMenu` tiene referencias serializadas para `OptionsMenu`, comics de `Cómo jugar`, boton de tutorial, boton siguiente y paginas del tutorial; `playSceneName` y `shopMenuSceneName` no apuntan a la escena tutorial pendiente.
- `ShopMenu` tiene cableadas las referencias serializadas de `OutOfGameShopManager`: botones, textos principales, estados visuales del dealer, vistas de slots y gotas del indicador de nivel.
- `ShopMenu` mantiene los botones y el contador bajo `ShopSafeArea/ShopContent`, con ajuste uniforme móvil; el fondo proporcional permanece fuera. La navegación Cancel referencia MainMenu, opciones y el módulo UI de la escena.
- Cada escena jugable tiene un unico `GameSessionController`, `RunProgressionDirector`, `SceneFlowController`, `LevelSpawner` y `GameUIRoot`.
- `LevelSpawner.zoneSpawnProfile` esta asignado.
- `GameUIRoot` tiene asignadas sus referencias serializadas de composicion, HUD y managers.
- Cada GameRoot activo contiene un unico `SafeAreaRoot` bajo HUD; todos los elementos HUD y la instancia touch canónica viven dentro de esa raíz, sin Canvas o EventSystem adicionales.
- Los cinco Canvas de gameplay conservan referencia 1920x1080 y escala por altura para no reducir el frame vertical en relaciones extra-wide.
- `MobileGameRootCatalog` descubre todos los prefabs `GameRoot_Zona*` bajo la carpeta de composición, excluye tutorial y comparte el inventario con las pruebas. Epipelágica y Abisopelágica siguen siendo obligatorias.
- `MobileHudLayoutContract` compara los cinco contenedores HUD con el layout de `GameRoot_ZonaEpipelagica`: anchors, posición, tamaño, pivot, escala, rotación y fuente de prefab anidada. La comparación no incluye sprites ni efectos internos de llenado.
- El contrato móvil se comprueba también sobre las instancias de las escenas jugables, para detectar overrides locales que no aparecen al inspeccionar sólo el prefab. `SafeAreaAdapter` debe actuar sobre su propio `SafeAreaRoot`.
- La raíz segura debe cubrir el HUD en autoría, sin offsets, escala adicional ni rotación. Las sustituciones locales de InkBar, gadgets, score o camarones también deben vivir bajo esa raíz y cumplir el layout común.
- El conteo de cinco Canvas base excluye el prefab separado `OptionsMenu`, añadido en las escenas; su adaptación móvil pertenece al contrato de opciones. Los controles touch siguen sin poder añadir infraestructura Canvas propia.
- El conteo de `SafeAreaAdapter` del HUD se limita a `HudRoot`; el overlay de opciones conserva su propia área segura. El prefab compartido y sus cuatro instancias deben mantener `OptionsSafeArea/OptionsContent`, el ajuste de referencia móvil, los controles dentro de ese contenido y un fondo que bloquee raycasts fuera de él.
- Pausa y Game Over mantienen respectivamente `PauseSafeArea/PauseContent` y `GameOverSafeArea/GameOverContent` en sus prefabs y en las dos zonas. El contrato compartido exige stretch de autoría, ajuste móvil 1920×1080, botones/decoración/puntajes dentro del contenido y fondo completo fuera del área segura, con un solo Canvas/raycaster y ningún EventSystem adicional.
- La tienda de run mantiene `InGameShopSafeArea/InGameShopContent`, tres hints de teclado cableados, indicación/contador móviles ocultos de autoría y `SinSaldo` fuera del estado Normal del botón. El prefab y ambas zonas comparten el contrato; `timerText` es obligatorio en las instancias de producto. Fondo completo e infraestructura UI se conservan.
- El prefab narrativo y sus instancias de MainMenu y ambas zonas mantienen `Comic/LoreSafeArea/LoreContent`, ajuste móvil y foco de continuar enlazados a las referencias reales del presenter. La viñeta conserva proporción y comparte contenido seguro con el botón; `Dimmer` queda fuera, sin otro Canvas/raycaster/EventSystem. El conteo de cuatro áreas del MainMenu excluye este overlay, que tiene su contrato independiente igual que opciones.
- `InGameShopManager` tiene asignadas sus referencias serializadas de tienda temporal: sesion, root, `CanvasGroup`, icono, textos, boton de compra, mensaje de saldo y ofertas con `GadgetShopItem`.
- Existe exactamente un objeto con tag `Player`.
- `Boundaries` contiene `PlayerBoundaries` y `CameraBoundaries`, cada uno con `TopBoundary` y `BottomBoundary` con `Collider2D`.
- Existe exactamente un `DestroyOffscreen` bajo `CleanUp`.
- Cada zona tiene un `BossEventDirector` cuyo prefab corresponde a la zona.
- `DealerFish` y `ScenePortal` no estan fijos en escenas jugables; deben nacer desde `LevelSpawner`.

## Busquedas globales restantes

Estas busquedas quedan documentadas como deuda controlada. No deben multiplicarse sin actualizar este contrato.

| Ubicacion | Busqueda | Clasificacion | Justificacion actual |
| --- | --- | --- | --- |
| `BoundaryReferenceResolver` | `FindObjectsByType<Transform>` | contrato estructural de escena | Resuelve `PlayerBoundaries` y `CameraBoundaries` por jerarquia documentada. |
| `DestroyOffscreen` | `GameObject.FindGameObjectsWithTag` | fallback de limpieza | Barrido de seguridad para objetos limpiables que no entraron al trigger. |
| `LevelSpawner` | `FindGameObjectWithTag(Player)` | dependencia de escena | El spawner necesita posicion del jugador; debe pasar a referencia serializada en un refactor posterior. |
| `CameraController` | `FindGameObjectWithTag(Player)` | dependencia de escena | La camara sigue al jugador canonico; debe pasar a contrato explicito de `CameraRig` o escena. |
| `SSCarnageController` | `FindGameObjectWithTag(Player)` | dependencia de escena | El boss necesita ubicar al jugador; se acepta mientras los bosses sigan naciendo por prefab. |
| `FishingRodEnemy` | `FindGameObjectWithTag(Player)` | entidad spawneada | Enemigo spawneado que necesita objetivo; preferir inyeccion por `EnemySpawnContext` en refactor posterior. |
| `PufferfishEnemy` | `FindGameObjectWithTag(Player)` | entidad spawneada | Igual que `FishingRodEnemy`; debe migrar gradualmente a contexto inyectado. |
| `InkPulseController` | `FindFirstObjectByType<ChargeBar>` | fallback de compatibilidad | El HUD debe venir desde `GameUIRoot`; se mantiene como respaldo mientras se refactoriza UI. |
| `InkPulseMusicCrossfader` | `FindFirstObjectByType<InkPulseController>` | fallback de compatibilidad | AudioRoot debe recibir referencia explicita en una tarea posterior. |
| `PauseMenuManager` | busqueda local de `OptionsMenuManager` en la escena cargada | fallback de compatibilidad UI | Los prefabs `GameRoot_*` aun no serializan `optionsMenu`; la busqueda queda acotada a roots de la escena activa y debe eliminarse cuando `GameUIRoot` exponga o serialice el menu de opciones. |
| `ScenePortal` | `FindFirstObjectByType<SceneFlowController>` | fallback de entidad spawneada | El portal nace desde `LevelSpawner`; idealmente recibira contexto de escena al instanciarse. |
| `FlappyBossController` | `FindFirstObjectByType<LevelSpawner>` | fallback de boss | Boss abisal necesita suspender/reanudar spawner; debe recibir contexto desde `BossEventDirector`. |
| `FairModeMenuManager` | `FindFirstObjectByType<EventSystem>` | bootstrap opcional de feria | Modo feria es opcional y crea UI auxiliar solo si falta infraestructura. |
| `PlayerSkinApplier` | `Resources.Load<GameObject>` | runtime store formal | Carga skins declaradas por `unlockables-catalog.json`; protegido por `CatalogIntegrityTests`. |
| `OutOfGameShopManager` | `Resources.Load<Sprite>` | runtime store formal | Carga sprites declarados por catalogo runtime; la vista de `ShopMenu` queda cableada por referencias serializadas validadas por `SceneCompositionValidator`. |
| `TutorialDirector` | `FindFirstObjectByType` y `FindGameObjectWithTag` | tutorial pendiente aislado | No pertenece al build activo; se tratara cuando se implemente el tutorial jugable. |
| `TutorialTaskHudController` | `FindFirstObjectByType<TutorialDirector>` | tutorial pendiente aislado | Igual que `TutorialDirector`. |

## Regla de evolucion

Una busqueda global solo debe permanecer si pertenece a una de estas categorias:

1. resolucion estructural documentada de escena;
2. entidad spawneada que aun no recibe contexto inyectado;
3. runtime store formal validado por pruebas;
4. bootstrap opcional de feria;
5. fallback temporal documentado.

Cuando un refactor elimine una busqueda, este documento debe actualizarse junto con la validacion o las pruebas que cubren el nuevo contrato.
