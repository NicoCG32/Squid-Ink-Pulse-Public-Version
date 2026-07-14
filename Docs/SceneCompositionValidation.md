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
- `ShopMenu` tiene cableadas las referencias serializadas de `OutOfGameShopManager`: botones, textos principales, estados visuales del dealer, vistas de slots y gotas del indicador de nivel.
- Cada escena jugable tiene un unico `GameSessionController`, `RunProgressionDirector`, `SceneFlowController`, `LevelSpawner` y `GameUIRoot`.
- `LevelSpawner.zoneSpawnProfile` esta asignado.
- `GameUIRoot` tiene asignadas sus referencias serializadas de composicion, HUD y managers.
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
