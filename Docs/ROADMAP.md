# Hoja de ruta

## Alcance

Este documento registra lo que falta para llevar Squid Ink-Pulse desde el estado actual hacia una estructura mas mantenible y preparada para expansion. No reemplaza a `StateMachines.md`, `RuntimeHierarchyAudit.md` ni `QATester.md`; los coordina desde una perspectiva de prioridades.

El salto de diseno actual tiene cinco frentes:

1. Consolidar al jugador como prefab reutilizable.
2. Implementar el nivel tutorial.
3. Implementar menu de opciones global.
4. Implementar tienda out-of-game de mejoras.
5. Implementar menu de opciones in-game reducido.

## Estado implementado

### Base de gameplay

Implementado:
- Movimiento del jugador con avance continuo y limites por `PlayerBoundaries`.
- Camara con limites por `CameraBoundaries`.
- Ink-Pulse formalizado como `Idle`, `Charging`, `Ready` y `Active`.
- Animacion visual de Ink-Pulse separada entre `SquidVisual` e `InkPulseVisual`.
- Jugador canonico como prefab `Assets/Content/Prefabs/Player/BabySquid.prefab`.
- `ZonaEpipelagica`, `ZonaExe` y `ZonaTutorial` usan instancias de ese prefab bajo el nodo `Squid`.
- Persistencia runtime de Ink-Pulse y gadgets entre portales.
- Reinicio de Ink-Pulse y gadgets al entrar en `GameSessionState.GameOver`.
- HUD de camarones, carga de Ink-Pulse y slots de gadgets.

Pendiente:
- Preparar variantes visuales del jugador para futuras skins.
- Agregar validacion automatizada para detectar instancias de jugador no conectadas al prefab canonico.

### Boundaries

Implementado:
- `BoundaryReferenceResolver` resuelve por dominio exacto: `PlayerBoundaries` o `CameraBoundaries`.
- Los nodos obligatorios son `TopBoundary` y `BottomBoundary`.
- `PlayerMovement`, `LevelSpawner`, `CameraController`, `BossEventDirector`, `SSCarnageController` y `SSCarnageNetWall` dejaron de depender de limites serializados manualmente.
- Se eliminaron rangos de respaldo (`fallbackMinY`, `fallbackMaxY`, `minY`, `maxY`) y offsets manuales de top boundary.

Pendiente:
- Agregar validacion automatizada en editor o Play Mode para detectar jerarquias de boundaries rotas antes de ejecutar QA.
- Crear plantilla de zona nueva con `PlayerBoundaries` y `CameraBoundaries` ya incluidos.

### Portales de zona

Implementado:
- `ScenePortal` cambia entre `ZonaEpipelagica` y `ZonaExe`.
- `LevelSpawner` instancia portales como evento de mundo.
- `ZonaEpipelagica` usa `PortalSpawnPolicy.PostBossWindow`.
- `ZonaExe` usa `PortalSpawnPolicy.AlwaysInterval`, con aparicion cada `20s`.
- Cruzar un portal conserva `RuntimeGadgetInventory` y `RuntimeInkPulseState`.
- `PlayerRuntimeState.PortalTransition` reproduce `PortalEffect` antes de cargar la escena y da prioridad visual a `PortalVisual`.

Pendiente:
- Evaluar una maquina `PortalTransitionState` separada solo si se agregan fases internas como fundido, carga asincronica o salida.
- Diferenciar UX visual y sonora por zona.
- Definir reglas de retorno segun progresion, no solo por escena activa.

### Tienda in-game

Implementado:
- `DealerFish` aparece como entidad de mundo.
- `InGameShopManager` muestra oferta temporal, precio y compra con `B`.
- Los gadgets se compran; no se recogen directamente.
- El inventario impide stacks.

Pendiente:
- Hacer que la aparicion de tienda dependa de progresion y eventos, no solo de intervalos.
- Balancear precios con avance real de la run.
- Mantenerla separada de la futura tienda out-of-game.

### Enemigos, boss y zonas

Implementado:
- Perfiles de spawn por enemigo.
- Tags formales para `EnemyPezGlobo`, `EnemyMina` y `EnemyCanaPescar`.
- SS Carnage y red como evento de boss integrado con progresion.
- Spawn regular aumenta durante `BossActive` y baja durante `PostBossWindow`.
- `ZonaExe` tiene oscuridad ambiental mediante `ZoneLightingController` y `LightGrazeSource`.

Pendiente:
- Completar comportamiento propio de mina y cana.
- Expandir variantes de enemigos y bosses segun el informe.
- Definir enemigos o patrones propios de `ZonaExe`.
- Balancear pesos, intensidades y multiplicadores por zona.

## Prioridad P0: Player como prefab

Estado: implementado como base estructural. Antes de escalar tutorial, skins, tienda global o mas zonas, el jugador ya existe como prefab canonico y las escenas jugables usan instancias de ese prefab.

### Objetivo

Mantener un prefab de jugador canonico:

```text
Assets/Content/Prefabs/Player/BabySquid.prefab
```

Jerarquia esperada:

```text
BabySquid
|-- GrazeZone
|-- SquidVisual
`-- InkPulseVisual
```

### Por que es critico

- Evita que `ZonaEpipelagica`, `ZonaExe` y `ZonaTutorial` tengan copias divergentes del jugador.
- Permite implementar skins sin reconstruir cada escena.
- Permite que tutorial y zonas compartan exactamente el mismo contrato visual y mecanico.
- Reduce errores al modificar `GrazeZone`, collider, `SquidVisual`, `InkPulseVisual` o inventario.
- Prepara variantes controladas mediante prefab variants, no mediante cambios manuales por escena.

### Reglas de arquitectura

- El prefab no debe guardar referencias directas a objetos de escena como `GameSession`, camara, HUD, boundaries o managers.
- Las referencias de escena deben resolverse por contrato o inyectarse desde un controlador de escena.
- El prefab puede contener componentes propios del jugador: movimiento, colision, inventario, Ink-Pulse, graze y visuales.
- La escena decide donde aparece el jugador mediante la instancia `Squid`; el prefab conserva reglas, collider y visuales base.
- Las skins futuras deben cambiar visuales, no reglas de movimiento ni colision.

### Implementado

1. `BabySquid.prefab` creado desde la jerarquia actual del jugador.
2. Dependencias de escena resueltas en runtime por los componentes del jugador cuando el prefab no serializa referencias externas.
3. Copias manuales de `Squid` reemplazadas por instancias de prefab en `ZonaEpipelagica`, `ZonaExe` y `ZonaTutorial`.
4. `ZonaExe` conserva `LightGrazeSource` como override de escena, porque esa capacidad visual pertenece a la zona y no al prefab base.
5. Contrato final documentado en `RuntimeHierarchyAudit.md`, `AssetFlow.md` y `AnimationStandards.md`.

### Trabajo pendiente derivado

1. Crear prefab variants o overrides visuales para skins.
2. Agregar validacion de editor que falle si una zona jugable contiene un `Squid` que no sea instancia de `BabySquid.prefab`.
3. Definir un `PlayerSpawnPoint` o controlador equivalente si el tutorial necesita posiciones pedagogicas no aleatorias.

## Prioridad P0: Nivel tutorial

El tutorial es la segunda prioridad importante, pero conviene implementarlo despues de convertir al jugador en prefab. Asi el tutorial ensena el comportamiento real del jugador y no una copia temporal.

### Objetivo

Implementar `ZonaTutorial` como secuencia guiada:

1. Movimiento.
2. Graze.
3. Carga y activacion de Ink-Pulse.
4. Tienda temporal.
5. Compra y uso de gadgets.
6. Boss/pared.
7. Portal hacia zona principal.

### Controlador requerido

Crear un `TutorialDirector` dedicado.

Responsabilidades:
- Activar pasos en orden.
- Bloquear o habilitar mecanicas segun el paso activo.
- Solicitar spawns tutorializados.
- Medir criterios de avance.
- Mostrar UI de tutorial si se requiere.
- Terminar cargando `ZonaEpipelagica` mediante `SceneFlowController`.

Reglas:
- No meter excepciones pedagogicas en `LevelSpawner` si solo existen para tutorial.
- No alterar `RunProgressionDirector` para ensenar mecanicas.
- No duplicar el jugador en escena.
- No usar enemigos reales de progresion si el paso requiere una version controlada.

## Prioridad P1: Menu de opciones global

`OptionsMenu` es un menu out-of-game. Debe afectar configuracion general del juego, no una run puntual.

### Opciones previstas

- Pantalla: modo ventana, fullscreen, resolucion si aplica.
- Brillo: ajuste visual global o multiplicador de postproceso/overlay.
- Volumen: master, musica y efectos.
- Dificultad: perfil de dificultad inicial o escalado base.

### Arquitectura recomendada

Crear un servicio/modelo de configuracion compartido, por ejemplo:

```text
GameSettings
AudioSettingsController
DisplaySettingsController
DifficultySettings
```

Reglas:
- El menu global puede modificar dificultad antes de iniciar partida.
- La dificultad no debe ser un campo suelto en spawners; debe alimentar progresion desde un modelo central.
- La persistencia real puede empezar con runtime y luego pasar a almacenamiento local.
- `OptionsMenu` no debe duplicar logica con el menu de pausa.

## Prioridad P1: Tienda out-of-game

`ShopMenu` debe ser una tienda global de mejoras permanentes, no una version grande de la tienda temporal.

### Objetivo

Permitir invertir camarones acumulados fuera de la run en:

- mejoras de skills;
- bonificaciones permanentes;
- mejoras de economia;
- skins;
- posibles modificadores iniciales de run.

### Dependencias

Antes de implementarla conviene tener:

1. Player prefab, para que skins tengan una base clara.
2. Persistencia durable de camarones, no solo runtime.
3. Un modelo de perfil del jugador.
4. Catalogo de mejoras y costos.

### Arquitectura recomendada

Componentes futuros:
- `PlayerProfile`: dinero persistente, upgrades comprados y skin activa.
- `UpgradeCatalog`: definiciones de mejoras.
- `OutGameShopManager`: UI y compra.
- `UpgradeEffectResolver`: aplica efectos al iniciar una run.

Reglas:
- No mezclar esta tienda con `InGameShopManager`.
- No vender gadgets temporales aqui si pertenecen a la run.
- No aplicar upgrades directamente desde botones sin pasar por un modelo de perfil.
- Las skins deben cambiar prefab variant, visual o override, no el controlador de gameplay.

## Prioridad P1: Menu de opciones in-game

El menu de opciones dentro de pausa debe ser una version reducida.

### Opciones permitidas

- Volumen.
- Pantalla.

### Opciones no recomendadas dentro de run

- Dificultad.
- Brillo si afecta lectura competitiva de la run de forma brusca.
- Reglas de progresion.

La razon es de coherencia: una run en curso debe tener reglas estables. Si se permite cambiar dificultad en pausa, debe tratarse como decision explicita de diseno y documentarse como excepcion.

### Integracion

- El boton `Opciones` del `PauseMenuManager` debe abrir un subpanel o escena overlay ligera.
- Debe reutilizar el mismo modelo de settings que `OptionsMenu`.
- No debe crear un segundo sistema de volumen o pantalla.

## Prioridad P2: Continuidad de sistemas existentes

Despues de los bloques anteriores, quedan estas mejoras de continuidad:

1. Transicion de portal avanzada con fundido, carga asincronica y salida.
2. Aparicion de tienda y portales basada en progresion mas expresiva.
3. Comportamiento completo de mina y cana.
4. Variantes de enemigos y bosses del informe.
5. Balance de `ZonaExe`, incluyendo oscuridad, patrones y audio.
6. Validaciones automaticas para boundaries, prefabs criticos y escenas.
7. Persistencia fuera de runtime para camarones, settings y perfil.

## Orden recomendado

1. Player como prefab.
2. Reemplazo del jugador en todas las zonas.
3. Tutorial con `TutorialDirector`.
4. Modelo compartido de settings.
5. `OptionsMenu` global.
6. Opciones in-game reducidas desde pausa.
7. Persistencia durable de perfil/camarones.
8. `ShopMenu` out-of-game de mejoras y skins.
9. Portales con transicion formal.
10. Expansion de enemigos, bosses y zonas.

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

## Criterio de mantenimiento

- Cuando una idea se vuelva implementacion real, se mueve a su documento de sistema correspondiente.
- Ninguna mecanica futura debe nacer como un `bool` suelto si necesita reglas claras de entrada y salida.
- Toda zona nueva debe declarar primero sus `PlayerBoundaries` y `CameraBoundaries`; despues se configuran spawns, camara, boss y portales.
- Los parametros editables deben pertenecer al manager o controlador dueno de la responsabilidad.
- Los prefabs no deben contener referencias directas a objetos de escena.
- Los informes en `Docs/Reports/` conservan caracter historico y no deben reescribirse para reflejar cada cambio de implementacion.
