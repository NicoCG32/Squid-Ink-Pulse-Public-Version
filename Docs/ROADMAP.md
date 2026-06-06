# Hoja de ruta

## Alcance

Este documento registra la direccion funcional del proyecto despues de la implementacion inicial de gadgets, tienda, portales, persistencia runtime y contrato formal de boundaries. No reemplaza a `StateMachines.md`; lo complementa con prioridades tecnicas y reglas de arquitectura.

## Estado implementado

### Boundaries

Implementado:
- `BoundaryReferenceResolver` resuelve por dominio exacto: `PlayerBoundaries` o `CameraBoundaries`.
- Los nodos obligatorios son `TopBoundary` y `BottomBoundary`.
- `PlayerMovement`, `LevelSpawner`, `CameraController`, `BossEventDirector`, `SSCarnageController` y `SSCarnageNetWall` dejaron de depender de limites serializados manualmente.
- Se eliminaron rangos de respaldo (`fallbackMinY`, `fallbackMaxY`, `minY`, `maxY`) y offsets manuales de top boundary.
- Escenas y prefabs fueron limpiados de campos legacy de boundaries.

Pendiente:
- Agregar validacion automatizada en editor o Play Mode para detectar jerarquias de boundaries rotas antes de ejecutar QA.
- Crear plantilla de zona nueva con `PlayerBoundaries` y `CameraBoundaries` ya incluidos.

### Portales de zona

Implementado:
- `ScenePortal` cambia entre `ZonaEpipelagica` y `ZonaExe`.
- `LevelSpawner` instancia portales como evento de mundo.
- `ZonaEpipelagica` usa `PortalSpawnPolicy.PostBossWindow`.
- `ZonaEpipelagica` espera `firstPortalSpawnDelay` tras boss y evalua `postBossPortalSpawnChance`.
- `ZonaExe` usa `PortalSpawnPolicy.AlwaysInterval`, con aparicion cada `20s`.
- Cruzar un portal conserva `RuntimeGadgetInventory` y `RuntimeInkPulseState`.
- Entrar en `GameSessionState.GameOver` reinicia gadgets e Ink-Pulse.

Pendiente:
- Formalizar `PortalTransitionState` para entrada, bloqueo de input, fundido, carga y salida.
- Diferenciar UX visual y sonora por zona.
- Definir reglas de retorno segun progresion, no solo por escena activa.

### Tutorial y menus globales

Preparado:
- `SceneFlowController` conoce `ZonaTutorial`, `ShopMenu` y `OptionsMenu`.
- Las escenas estan registradas en Build Settings para futura conexion desde UI.
- `ZonaTutorial` queda definida como secuencia futura: movimiento, graze, Ink-Pulse, tienda, gadgets, boss/pared y portal.

Pendiente:
- Implementar `TutorialDirector` con pasos, locks de mecanicas y criterios de avance.
- Conectar `ShopMenu` como tienda global de skills, skins y bonificaciones.
- Conectar `OptionsMenu` para volumen, pantalla y dificultad.

### Diferenciacion de ZonaExe

Implementado:
- `ZonaExe` tiene oscuridad ambiental mediante `ZoneLightingController`.
- El fondo no se duplica: `LayerBlack` oscurece la escena real.
- `LayerBlack` se perfora localmente mediante `SpriteMask`.
- `LightGrazeSource` crea mascaras circulares alrededor de entidades.
- BabySquid tiene `LightGrazeSource` en `ZonaExe`.
- `LevelSpawner` agrega `LightGrazeSource` a enemigos, camarones, `DealerFish` y portales solo si existe `ZoneLightingController`.

Pendiente:
- Balancear `blackAlpha`, `lightHoleRadius`, `maskAlphaCutoff` y cobertura visual.
- Agregar feedback sonoro/particulas si el revelado necesita mayor legibilidad.
- Definir enemigos o patrones propios de zona que aprovechen la oscuridad.

### Tienda in-game

Implementado:
- `DealerFish` aparece como entidad de mundo.
- `InGameShopManager` muestra oferta temporal, precio y compra con `B`.
- Los gadgets se compran; no se recogen directamente.
- El inventario impide stacks.

Pendiente:
- Hacer que la aparicion de tienda dependa de progresion y eventos, no solo de intervalos.
- Formalizar mejor el estado de evento de tienda si se agregan fases nuevas.
- Balancear precios con el avance real de la run.

### Gadgets

Implementado:
- `Shell Shield` como gadget pasivo de salvavidas.
- `Ink-Bottle` como gadget activo que fuerza Ink-Pulse a `Ready`.
- Slots runtime por orden de adquisicion: `Gadget1 = Q`, `Gadget2 = W`.
- HUD de inventario persistente entre portales.

Pendiente:
- Formalizar `GadgetRuntimeState` solo cuando existan cooldowns, duraciones o fases propias.
- Agregar nuevos gadgets del informe.
- Agregar feedback visual/sonoro por activacion y consumo.

### Enemigos y boss

Implementado:
- Perfiles de spawn por enemigo.
- Tags formales para `EnemyPezGlobo`, `EnemyMina` y `EnemyCanaPescar`.
- SS Carnage y red como evento de boss integrado con progresion.
- Spawn regular aumenta durante `BossActive` y baja durante `PostBossWindow`.

Pendiente:
- Completar comportamiento propio de mina y cana.
- Expandir variantes de enemigos y bosses segun el informe.
- Balancear pesos, intensidades y multiplicadores por zona.

## Invariante de boundaries

Esta regla aplica a `ZonaEpipelagica`, `ZonaExe`, `ZonaTutorial` y cualquier zona futura.

| Dominio | Contenedor obligatorio | Nodos obligatorios |
| --- | --- | --- |
| Jugador | `PlayerBoundaries` | `TopBoundary`, `BottomBoundary` |
| Camara | `CameraBoundaries` | `TopBoundary`, `BottomBoundary` |

Reglas:
- No definir limites por valores manuales como fuente primaria.
- No depender de medidas sueltas en scripts para establecer el area jugable.
- No crear boundaries alternativos fuera de `PlayerBoundaries` y `CameraBoundaries`.
- Si falta un nodo obligatorio, debe tratarse como error de configuracion de escena.

## Proximas prioridades

1. Validacion automatica de jerarquias de zona y prefabs criticos.
2. Prueba de portales en Play Mode con persistencia de gadgets, Ink-Pulse y camarones.
3. Pulir UX de entrada/salida de zona mediante `PortalTransitionState`.
4. Mover aparicion de tienda y portales hacia reglas de progresion mas expresivas.
5. Completar algoritmos propios de mina y cana.
6. Balancear y expandir la diferenciacion de `ZonaExe`.
7. Implementar `TutorialDirector` y conectar `ShopMenu` / `OptionsMenu` cuando se definan sus menus.

## Criterio de mantenimiento

- Cuando una idea se vuelva implementacion real, se mueve a su documento de sistema correspondiente.
- Ninguna mecanica futura debe nacer como un `bool` suelto si necesita reglas claras de entrada y salida.
- Toda zona nueva debe declarar primero sus `PlayerBoundaries` y `CameraBoundaries`; despues se configuran spawns, camara, boss y portales.
- Los parametros editables deben pertenecer al manager o prefab dueno de la responsabilidad.
- Los informes en `Docs/Reports/` conservan caracter historico y no deben reescribirse para reflejar cada cambio de implementacion.
