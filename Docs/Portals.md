# Portales

## Alcance

Los portales permiten cambiar entre escenarios de gameplay sin pasar por menu ni terminar la run. La implementacion actual cubre la transicion directa entre `ZonaEpipelagica` y `ZonaAbisopelagica`.

El portal no es un objeto fijo de escena. Es un prefab instanciado por `LevelSpawner`, igual que otros eventos del mundo.

## Archivos

- `Assets/Implementation/Code/World/Portals/ScenePortal.cs`
- `Assets/Implementation/Code/Spawning/LevelSpawner.cs`
- `Assets/Content/Prefabs/Portals/ScenePortal.prefab`
- `Assets/Scenes/Game/ZonaEpipelagica.unity`
- `Assets/Scenes/Game/ZonaAbisopelagica.unity`
- `ProjectSettings/EditorBuildSettings.asset`

## Responsabilidades

`ScenePortal`:
- Detecta contacto con el jugador usando `GameplayTagCatalog.Player`.
- Usa tag `Portal` y capa `Collectible`.
- Deshabilita sus colliders al activarse para evitar doble carga.
- Solicita a `PlayerStateController` entrar en `PlayerRuntimeState.PortalTransition`.
- Espera la duracion de `PlayerVisualStateController.PortalTransitionDuration`.
- Solicita a `SceneFlowController` cargar el destino correspondiente al terminar `PortalEffect`.

`PlayerVisualStateController`:
- Vive en el root de `BabySquid`.
- Muestra `PortalVisual` durante `PlayerRuntimeState.PortalTransition`.
- Oculta `SquidVisual` e `InkPulseVisual` mientras `PortalVisual` esta activo.
- Usa la duracion real del clip/controlador como fuente de espera para `ScenePortal`.

`SceneFlowController`:
- Conserva las rutas de zona configurables.
- Decide la escena destino segun la escena activa.
- Resuelve rutas `.unity` a nombres disponibles en Build Settings.

`LevelSpawner`:
- Instancia `ScenePortal.prefab` como evento temporal de mundo.
- Ubica el portal por la derecha de la camara.
- Elige la altura dentro del rango permitido por `PlayerBoundaries`.
- Aplica tag `Portal` y capa `Collectible` de forma recursiva.
- Puede exigir que no exista otro portal activo antes de crear uno nuevo.
- En `PostBossWindow`, espera `firstPortalSpawnDelay`, realiza una sola tirada con `postBossPortalSpawnChance` y no reintenta hasta el siguiente boss.

## Politicas de aparicion

`LevelSpawner` usa `PortalSpawnPolicy`:

| Valor | Uso |
| --- | --- |
| `Disabled` | No instancia portales. |
| `PostBossWindow` | Evalua un portal solo mientras `RunProgressionDirector.EventState` sea `PostBossWindow`, despues de un delay y con probabilidad configurable. |
| `AlwaysInterval` | Instancia portales por intervalo, sin depender del boss. |

Configuracion actual:

| Escena | Politica | Delay | Probabilidad | Intervalo |
| --- | --- | --- | --- | --- |
| `ZonaEpipelagica` | `PostBossWindow` | `3s` tras resolver boss | `1.0` por defecto | no reintenta en la misma ventana |
| `ZonaAbisopelagica` | `AlwaysInterval` | `20s` | no aplica | `20s` |

## Regla de ida y vuelta

Las rutas no pertenecen al prefab del portal. Se configuran en `SceneFlowController`:

| Campo | Uso |
| --- | --- |
| `primaryGameplaySceneName` | Zona base o retorno. Por defecto: `Assets/Scenes/Game/ZonaEpipelagica.unity`. |
| `secondaryGameplaySceneName` | Zona alterna. Por defecto: `Assets/Scenes/Game/ZonaAbisopelagica.unity`. |

Regla actual:
- Si la escena activa coincide con `secondaryGameplaySceneName`, el portal vuelve a `primaryGameplaySceneName`.
- Si la escena activa no coincide con `secondaryGameplaySceneName`, el portal entra a `secondaryGameplaySceneName`.

Esto permite usar el mismo prefab como portal de ida y portal inverso.

## Persistencia entre portales

Cruzar un portal no equivale a Game Over:
- `RuntimeGadgetInventory` conserva gadgets y slots.
- `RuntimeInkPulseState` conserva carga, estado activo y tiempo restante.
- `RuntimeRunScore` conserva el puntaje acumulado de la run.
- `RuntimePlayerPace` conserva la progresion de velocidad.
- `GameSessionController` reinicia esos estados de run solo al entrar en `GameSessionState.GameOver`.
- `ShrimpRuntimeWallet` conserva camarones durante runtime.
- `PersistentPlayerProfile` incrementa `player-records.json.totalPortalsCrossed` cuando el portal acepta cargar la escena destino.
- Si el jugador entra al portal durante `InkPulseState.Active`, `PortalVisual` tiene prioridad visual, pero el estado runtime de Ink-Pulse sigue persistiendo hacia la escena siguiente.

## Contrato de zona

Para que un portal pueda aparecer, la zona debe tener:
- `PlayerBoundaries/TopBoundary`
- `PlayerBoundaries/BottomBoundary`
- `CameraBoundaries/TopBoundary`
- `CameraBoundaries/BottomBoundary`
- `LevelSpawner.zoneSpawnProfile` con `portalPrefab` configurado.
- `ProjectSettings/EditorBuildSettings.asset` con la escena destino habilitada.

Si faltan boundaries, el portal no debe inventar una altura manual.

## Mantenimiento

- No colocar portales fijos `PortalTo...` en escena.
- No usar tag `Shrimp` en portales: el portal no debe sumar camarones.
- No usar tag `Collectible` en portales, aunque usen la capa `Collectible`; el tag debe ser `Portal`.
- No usar `DealerFish` para portales: tienda y portal son interacciones distintas.
- No poner rutas de escena en el prefab `ScenePortal`; esas rutas pertenecen a `SceneFlowController`.
- La animacion del jugador al cruzar portal pertenece a `PortalVisual` dentro de `BabySquid`; el prefab `ScenePortal` solo detecta contacto.
- No cargar la escena inmediatamente al tocar portal: primero debe entrar `PlayerRuntimeState.PortalTransition` y reproducirse `PortalEffect`.
