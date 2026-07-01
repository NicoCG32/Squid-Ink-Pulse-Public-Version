# Hoja de ruta

## Alcance

Este documento registra lo que falta para llevar Squid Ink-Pulse desde el estado actual hacia una estructura mas mantenible y preparada para expansion. No reemplaza a `StateMachines.md`, `RuntimeHierarchyAudit.md` ni `QATester.md`; los coordina desde una perspectiva de prioridades.

Para la semana final antes de feria, el enfoque cambia: no conviene priorizar expansion general, sino una version presencial estable, entendible, recuperable y presentable en varios PCs.

Objetivo de entrega de feria:

```text
3 PCs ejecutan el juego
|-- 1 PC puede actuar como host de leaderboard LAN
|-- los visitantes pueden jugar varios intentos
|-- su progreso de feria puede recuperarse
|-- al salir se sincroniza su mejor estado
|-- el ranking se puede visualizar en pantalla
```

Regla de alcance: cualquier refactor que no mejore directamente estabilidad, onboarding, tienda, comics, boss visible o leaderboard de feria queda fuera de la semana final.

## Roadmap de feria: semana final

Fecha de planificacion: 2026-06-25. Ventana estimada: 7 dias.

### P0: imprescindible para presentar

Estas tareas definen si el juego puede mostrarse con confianza en feria.

1. Build estable de demo.
   - Todas las escenas principales cargan sin referencias faltantes criticas.
   - `ZonaAbisopelagica` ya contiene `BossEventDirector` para `UnknownBoss` / `FlappyBoss`; el cierre pendiente es validarlo en Play Mode y en build.
   - MainMenu, gameplay, pausa, Game Over, opciones, ShopMenu y vuelta a menu funcionan; el usuario ya reporto correctamente gameplay, botones, pausa, Game Over, skins, tienda in-run y tienda out-of-run en Editor.
   - No hay bloqueos por `Time.timeScale`, pausa, comics, tienda o Game Over.
   - `ZonaEpipelagica` y `ZonaAbisopelagica` pueden jugarse durante varios minutos sin acumulacion evidente de objetos.
   - Criterio de cierre: build Windows probado fuera del Editor.

2. Tienda out-of-game funcional.
   - `ShopMenu` ya serializa seleccion, compra de mejoras, compra/equipado de skins, paginacion, precios compactos y estados visuales mediante `OutOfGameShopManager` y `PermanentShopService`.
   - Estado de prueba actual: compra, seleccion, deseleccion, skins y mejoras fueron reportadas como funcionales en gameplay. Mantener como pendiente solo build fuera del Editor y QA de regresion.
   - Debe cubrir como minimo:
     - `upgrade.ink_pulse_duration`;
     - `upgrade.ink_pulse_recharge_rate`;
     - `upgrade.shrimp_multiplier`;
     - `upgrade.score_multiplier`.
   - Debe mostrar saldo persistente con `ShrimpCounter`.
   - Debe persistir compras en `player-profile.json`/`player-records.json`.
   - Skins ya pueden comprarse y equiparse a nivel de perfil; el cambio visual del jugador queda P1 si no hay tiempo.
   - Criterio de cierre: comprar una mejora altera gameplay o economia observable sin editar codigo.

3. Servidor de feria MVP.
   - Implementado como subsistema externo en `Tools/FairServer/`, no como reemplazo de la DB normal.
   - Siguiente prioridad recomendada cuando el loop local ya esta estable.
   - Implementacion actual: Python estandar + SQLite, sin dependencias externas, para ejecucion simple en Windows.
   - MVP obligatorio:
     - crear participante con seudonimo: implementado;
     - generar codigo de recuperacion: implementado;
     - recuperar participante: implementado;
     - sincronizar snapshot: implementado;
     - checkout al salir: implementado;
     - consultar leaderboard: implementado;
     - pantalla web simple de ranking: implementado.
   - Integracion Unity minima:
     - exportar mejor puntaje, intentos, mejoras permanentes y desbloqueos relevantes;
     - importar snapshot recuperado;
     - mostrar puesto al salir.
   - Criterio pendiente de cierre: dos PCs pueden crear/recuperar/sincronizar contra el host en LAN desde build Unity.

4. Comics de lore criticos.
   - Validar que los comics ya conectados se muestran y no bloquean:
     - inicio al presionar Play;
     - portal Epi -> Abi;
     - portal Abi -> Epi;
     - derrota por zona;
     - primera entrada a tienda in-game;
     - salida de tienda in-game con compra y sin compra.
   - No implementar todavia comics por hitos de puntaje.
   - No crear UI visual por runtime; solo corregir referencias si falta algo.
   - Criterio de cierre: cada flujo se prueba en Play Mode y puede continuar despues del comic.

5. Visual minimo del segundo boss.
   - `UnknownBoss` / `FlappyBoss` no puede sentirse invisible o placeholder roto si aparece en la feria.
   - Ya esta conectado en `ZonaAbisopelagica` y declara `LightGrazeSource` propio; falta validar lectura visual, ritmo y estabilidad en Play Mode.
   - Los pilares deben seguir funcionando con boundaries de camara y pared final continua.
   - La prioridad es legibilidad, no animacion final.
   - Criterio de cierre: el boss se entiende visualmente en `ZonaAbisopelagica` y no rompe FPS.

6. Tutorial o onboarding corto.
   - Si el tutorial completo no llega a estar pulido, preparar un modo de entrada breve o instrucciones externas para feria.
   - Prioridad tecnica: validar al menos movimiento, graze, Ink-Pulse, camarones, tienda y Game Over.
   - El tutorial completo sigue siendo importante, pero no debe bloquear el build de feria si el loop principal ya es entendible por monitoria presencial.
   - Criterio de cierre: un visitante nuevo entiende movimiento, Ink-Pulse y objetivo en menos de un minuto.

7. QA de estabilidad y performance.
   - Medir `ZonaAbisopelagica` porque ya fue el punto mas riesgoso.
   - Revisar:
     - cleanup de fondos/parallax;
     - cantidad de enemigos/minas/anzuelos/dealer;
     - `ZoneLightingController`;
     - boss abisal y pilares;
     - audio/Ink-Pulse;
     - pausa y Game Over.
   - Criterio de cierre: prueba de 20-30 minutos sin degradacion evidente ni jerarquia creciendo sin control.

### P1: alto valor si P0 esta cerrado

1. UI final de feria dentro de Unity.
   - Pantalla "Nuevo jugador".
   - Pantalla "Recuperar".
   - Pantalla "Tu puesto".
   - Mensajes de error si no hay servidor.

2. Skins en ShopMenu.
   - Solo si la infraestructura de compra permanente ya esta cerrada.
   - Debe cambiar visual, no mecanica.

3. Pulido de comics.
   - Ajuste de duracion por evento.
   - Verificar arte final y encuadre.
   - Causas de derrota solo si ya existe forma fiable de detectar causa.

4. Pulido del segundo boss.
   - Animacion de entrada/salida.
   - Feedback sonoro o VFX si no impacta performance.
   - Ajuste fino de ritmo de pilares.

5. OptionsMenu y botones.
   - Revisar escala y fondo oscuro en MainMenu, ShopMenu y escenas jugables.
   - Confirmar volumen/resolucion global.
   - Confirmar botones con contrato `Button` + `Visual`.

### P2: cortar si compite con la feria

Estas tareas pueden esperar aunque sean buenas para el proyecto.

- Comics por hitos de puntaje.
- Causas especificas de derrota con variantes narrativas.
- Refactor amplio de sistemas de persistencia.
- Nuevas zonas.
- Nuevos enemigos no necesarios para el recorrido de feria.
- Transiciones de portal asincronicas complejas.
- Editor tooling nuevo salvo validaciones muy concretas.
- Rebalance fino de largo plazo.

### Plan por dias

El orden propuesto reduce riesgo: primero cerrar jugabilidad y datos, luego presentacion, luego feria LAN, luego polish.

| Dia | Foco | Resultado esperado |
| --- | --- | --- |
| D-7 | Auditoria corta + ShopMenu | Lista final de faltantes, compra permanente minima funcionando. |
| D-6 | ShopMenu + comics | Mejoras impactan gameplay; comics criticos no bloquean. |
| D-5 | Servidor feria MVP | API SQLite + pantalla web leaderboard funcionando en local. |
| D-4 | Integracion Unity feria | Crear/recuperar/sync/checkout desde build o escena de prueba. |
| D-3 | Segundo boss + QA abisal | Boss visible y zona abisal estable durante prueba larga. |
| D-2 | Prueba 3 PCs | LAN real, host fijo, firewall, recuperacion, leaderboard. |
| D-1 | Freeze de build | Solo bugs criticos; preparar ejecutables, instrucciones y backup. |
| D-0 | Feria | No cambiar codigo salvo emergencia. |

### Criterios de aceptacion de feria

La version de feria se considera lista si:

- el juego arranca desde build en los PCs definidos;
- MainMenu permite jugar, opciones, tienda y salida sin errores visibles;
- `ShopMenu` permite comprar mejoras permanentes reales;
- los comics criticos aparecen sin cortar el flujo;
- el segundo boss tiene presencia visual comprensible;
- el host LAN recibe participantes y snapshots;
- el ranking se ve en pantalla externa o web local;
- un participante puede recuperar su progreso con seudonimo + codigo;
- si el servidor falla, el juego no se rompe;
- existe backup de la base SQLite antes de abrir la feria;
- se documenta la IP/puerto del host y el procedimiento de arranque.

### Corte de emergencia

Si faltan menos de 48 horas y algo sigue inestable, aplicar este recorte:

1. Mantener gameplay principal estable por sobre todo.
2. Mantener ShopMenu con mejoras y skins ya implementadas; desactivar compra/equipamiento de skins solo si el flujo visual introduce un bug critico de ultima hora.
3. Mantener servidor de feria con crear/sync/leaderboard; recuperacion manual puede quedar simplificada si existe fallback local.
4. Mantener comics de inicio, portal y derrota; comics de tienda pueden quedar desactivados si bloquean.
5. Mantener segundo boss visible aunque sea con visual simple.
6. Congelar balance; solo corregir bugs que impidan jugar.

## Backlog estructural post-feria

El salto de diseno general sigue teniendo estos frentes, pero quedan subordinados al roadmap de feria:

1. Consolidar al jugador como prefab reutilizable.
2. Implementar el nivel tutorial.
3. Implementar menu de opciones global.
4. Implementar tienda out-of-game de mejoras.
5. Implementar menu de opciones in-game reducido.

## Prioridad maxima estructural

Fuera del recorte de feria, las siguientes dos piezas siguen bloqueando la prueba completa del juego y deben tratarse como P0 hasta quedar terminadas:

1. `ZonaTutorial` con `TutorialDirector`.
2. `ShopMenu` como tienda out-of-game de progreso permanente.

El tutorial no es solo onboarding: debe funcionar como prueba integrada del loop principal. Debe validar movimiento, graze, Ink-Pulse, tienda temporal, gadgets, SS Carnage/red, Shell Shield, portal, cambio visual de zona y Game Over dentro de un flujo dirigido y repetible.

La tienda out-of-game no es una version grande de la tienda temporal: debe consumir la base persistente y los servicios permanentes para skins, upgrades, precios, niveles, saldo y efectos reales sobre gameplay.

## Estado implementado

### Base de gameplay

Implementado:
- Movimiento del jugador con avance continuo y limites por `PlayerBoundaries`.
- Camara con limites por `CameraBoundaries`.
- Ink-Pulse formalizado como `Idle`, `Charging`, `Ready` y `Active`.
- Animacion visual de Ink-Pulse separada entre `SquidVisual` e `InkPulseVisual`.
- Jugador canonico como prefab `Assets/Content/Prefabs/Player/BabySquid.prefab`.
- `ZonaEpipelagica`, `ZonaAbisopelagica` y `ZonaTutorial` usan instancias de ese prefab bajo el nodo `Squid`.
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
- `Assets/Content/Prefabs/World/Boundaries.prefab` es la fuente canonica de la jerarquia `Boundaries`.
- `ZonaEpipelagica`, `ZonaAbisopelagica` y `ZonaTutorial` usan instancias del prefab con overrides de colliders por zona.
- `PlayerMovement`, `LevelSpawner`, `CameraController`, `BossEventDirector`, `SSCarnageController` y `SSCarnageNetWall` dejaron de depender de limites serializados manualmente.
- Se eliminaron rangos serializados de respaldo (`fallbackMinY`, `fallbackMaxY`, `minY`, `maxY`) y offsets manuales de top boundary como fuente de configuracion.

Pendiente:
- Agregar validacion automatizada en editor o Play Mode para detectar jerarquias de boundaries rotas antes de ejecutar QA.
- Crear plantilla de zona nueva que incluya instancias de `Boundaries`, `CleanUp`, `GameUIRoot` y `BabySquid`.

### Portales de zona

Implementado:
- `ScenePortal` cambia entre `ZonaEpipelagica` y `ZonaAbisopelagica`.
- `LevelSpawner` instancia portales como evento de mundo.
- `ZonaEpipelagica` usa `PortalSpawnPolicy.PostBossWindow`.
- `ZonaAbisopelagica` usa `PortalSpawnPolicy.PostBossWindow`; el portal solo puede aparecer despues del boss y se decide con `postBossPortalSpawnChance`.
- Cruzar un portal conserva `RuntimeGadgetInventory` y `RuntimeInkPulseState`.
- `PlayerRuntimeState.PortalTransition` reproduce `PortalEffect` antes de cargar la escena y da prioridad visual a `PortalVisual`.

Pendiente:
- Evaluar una maquina `PortalTransitionState` separada solo si se agregan fases internas como fundido, carga asincronica o salida.
- Diferenciar UX visual y sonora por zona.
- Definir reglas de retorno segun progresion, no solo por escena activa.

### Lore comics

Implementado:
- `LoreComicPresenter` como orquestador runtime.
- `LoreComic.prefab` como overlay narrativo canonico.
- Comic de inicio solicitado desde `MainMenu.Jugar`.
- Comic de portal solicitado desde `ScenePortal` antes de cargar destino.
- Comic de derrota solicitado desde `GameOverMenuManager` antes de mostrar Game Over.
- Comics de primera entrada y primera salida de tienda in-game solicitados desde `InGameShopManager`.
- Vinetas reemplazables organizadas por dominio para inicio, portales, derrotas por zona y tienda in-game.
- Instancias `LoreComicRoot` en `MainMenu` y en `GameRoot_ZonaEpipelagica`, `GameRoot_ZonaAbisopelagica` y `GameRoot_ZonaTutorial`.

Pendiente:
- Validar arte final y referencias de vinetas.
- Validar inicio, ambos sentidos de portal y derrotas en Play Mode.
- Agregar causa de derrota si el diseno necesita comics distintos por forma de perder.
- Agregar comics por hitos de puntaje cuando el contrato de hitos este definido.

### Tienda in-game

Implementado:
- `DealerFish` aparece como entidad de mundo.
- `InGameShopManager` muestra oferta temporal, precio y compra con `B`.
- Los gadgets se compran; no se recogen directamente.
- El inventario impide stacks.
- Las ofertas se filtran por `RunGadgetUnlockService`; un gadget bloqueado por hito no aparece en la tienda temporal.

Pendiente:
- Hacer que la aparicion de tienda dependa de progresion y eventos, no solo de intervalos.
- Balancear precios con avance real de la run.
- Mantenerla separada de la futura tienda out-of-game.

### Tienda out-of-game

Implementado:
- `OutOfGameShopManager` conecta seleccion, compra y paginacion de la tienda permanente.
- Las cuatro mejoras permanentes tienen nivel maximo 10, precio creciente y visual de gotas de tinta.
- Las mejoras cargan sprite normal y sprite seleccionado `Ink` desde `Resources`.
- Las skins cargan imagen desde el catalogo, pueden comprarse y pueden equiparse en `player-profile.json`.
- El precio mostrado usa el formato compacto de `ShrimpCounterDisplay`.
- `ComprarBoton` ejecuta la transaccion real mediante `PermanentShopService`.

Pendiente:
- Validar en Play Mode que las compras sobreviven a reinicio cuando se desea persistencia.
- Validar el flujo de prueba limpia borrando `Application.persistentDataPath/db/`.
- Aplicar la skin equipada sobre el visual del jugador.
- Agregar feedback final de compra exitosa/fallida si el diseno visual lo requiere.

### Enemigos, boss y zonas

Implementado:
- Perfiles de spawn por enemigo.
- Tags formales para `EnemyPezGlobo`, `EnemyMina` y `EnemyCanaPescar`.
- SS Carnage y red como evento de boss integrado con progresion.
- `UnknownBoss` / `FlappyBoss` conectado como boss propio de `ZonaAbisopelagica`.
- Spawn regular aumenta durante `BossActive`; `PostBossWindow` mantiene presion alta salvo que el jugador cruce portal y reinicie la intensidad en otra zona.
- `ZonaAbisopelagica` tiene oscuridad ambiental mediante `ZoneLightingController` y `LightGrazeSource`.
- `LightGrazeSource` soporta ancla, forma eliptica y titileo para lectura visual de entidades abisales.

Pendiente:
- Completar comportamiento propio de mina y cana.
- Expandir variantes de enemigos y bosses segun el informe.
- Validar y balancear patrones propios de `ZonaAbisopelagica`, especialmente boss abisal y pilares.
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

- Evita que `ZonaEpipelagica`, `ZonaAbisopelagica` y `ZonaTutorial` tengan copias divergentes del jugador.
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
3. Copias manuales de `Squid` reemplazadas por instancias de prefab en `ZonaEpipelagica`, `ZonaAbisopelagica` y `ZonaTutorial`.
4. `ZonaAbisopelagica` conserva `LightGrazeSource` como override de escena, porque esa capacidad visual pertenece a la zona y no al prefab base.
5. Contrato final documentado en `RuntimeHierarchyAudit.md`, `AssetFlow.md` y `AnimationStandards.md`.

### Trabajo pendiente derivado

1. Crear prefab variants o overrides visuales para skins.
2. Agregar validacion de editor que falle si una zona jugable contiene un `Squid` que no sea instancia de `BabySquid.prefab`.
3. Definir un `PlayerSpawnPoint` o controlador equivalente si el tutorial necesita posiciones pedagogicas no aleatorias.

## Prioridad P0: Nivel tutorial

Estado: flujo mecanico implementado. `ZonaTutorial` usa el jugador canonico y contiene `TutorialDirector` sobre `GameSession` para gobernar la progresion pedagogica por `TutorialStep`.

### Objetivo

Implementar `ZonaTutorial` como secuencia guiada y prueba integrada:

1. Movimiento.
2. Graze para cargar Ink-Pulse.
3. Uso obligatorio de Ink-Pulse.
4. Recoleccion de 10 camarones.
5. Apertura de tienda temporal.
6. Compra de Ink Bottle como Gadget #1.
7. Ink Bottle activo dentro de la run, sin temporizador limitado.
8. SS Carnage y red.
9. Uso de Ink-Pulse para superar SS Carnage.
10. Segunda tienda.
11. Compra de Shell Shield.
12. Aparicion de enemigo.
13. Hit bloqueado por Shell Shield.
14. Portal.
15. Cambio visual/capa hacia segunda zona dentro de la misma escena.
16. Enemigo final.
17. Hit sin Shell Shield disponible.
18. Game Over.

### Controlador implementado

`TutorialDirector` dedicado.

Responsabilidades:
- Activar pasos en orden.
- Bloquear `LevelSpawner` y `BossEventDirector` durante la secuencia dirigida.
- Solicitar spawns tutorializados.
- Medir criterios de avance.
- Coordinar tienda temporal, gadgets, SS Carnage, portal local y Game Over.
- Exponer eventos para UI de tutorial futura, sin crear textos/prompts todavia.

Pasos actuales:
- `Movement`
- `GrazeCharge`
- `InkPulseObstacle`
- `CollectShrimps10`
- `FirstShopOpen`
- `BuyInkBottle`
- `InkBottleBarrier`
- `CarnageIntro`
- `CarnageInkPulseAssist`
- `CarnageInkPulseResolve`
- `SecondShopOpen`
- `BuyShellShield`
- `ProtectedHitSetup`
- `ProtectedHitResolved`
- `PortalSpawn`
- `PortalEnter`
- `VisualZoneShift`
- `FinalEnemy`
- `FinalDeath`
- `Completed`

Reglas:
- No meter excepciones pedagogicas en `LevelSpawner` si solo existen para tutorial.
- No alterar `RunProgressionDirector` para ensenar mecanicas.
- No duplicar el jugador en escena.
- No usar enemigos reales de progresion si el paso requiere una version controlada.

Pendiente:
- Validar el recorrido completo en Play Mode.
- Ajustar `firstZoneVisualRoots` y `secondZoneVisualRoots` cuando exista el arte/capa de segunda zona dentro de la escena.
- Mantener postergada la UI de instrucciones, textos y senalizacion visual hasta que el flujo mecanico sea aprobado.

## Prioridad P1: Menu de opciones global

`OptionsMenu` es un menu out-of-game. Debe afectar configuracion general del juego, no una run puntual.

Estado actual:
- `OptionsMenuManager` controla volumen master, resolucion y pantalla completa.
- Las preferencias se guardan en `PlayerPrefs`, separadas de la base persistente `db`.
- El prefab `OptionsMenu` tiene fondo visual propio y se instancia como root separado de escena.

### Opciones previstas fuera del alcance actual

- Brillo: ajuste visual global o multiplicador de postproceso/overlay.
- Volumen separado: master, musica y efectos.
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
- Settings debe tener almacenamiento propio; no pertenece a la base `db` de progreso.
- `OptionsMenu` no debe duplicar logica con el menu de pausa.

## Prioridad P0: Tienda out-of-game

`ShopMenu` debe ser una tienda global de mejoras permanentes, no una version grande de la tienda temporal.

### Objetivo

Permitir invertir camarones acumulados fuera de la run en dos subtiendas:

- skins;
- mejoras permanentes: duracion de Ink-Pulse, rate de recarga de Ink-Pulse, multiplicador de camarones y multiplicador de score.

### Dependencias

Base ya disponible:

1. Player prefab, para que skins tengan una base clara.
2. Base JSON local `db` con `unlockables-catalog.json`, `player-profile.json`, `player-records.json` y `local-leaderboard.json`.

Estado actual:

1. `OutOfGameShopManager` conecta cuatro slots de upgrades, cuatro slots paginados de skins, navegacion y compra con datos reales.
2. `ShrimpCounter` es una instancia del prefab en `ShopMenu`.
3. El catalogo actual contiene varias skins con sprites de tienda bajo `Assets/Content/Art/UI/ShopMenu/Resources/ShopMenu/Skins/`.
4. Los textos de nombre, descripcion y precio ya son parte del contrato funcional de `ProductInfoBlock`; su layout sigue siendo autoria visual de escena.
5. Las mejoras tienen sprites normales y seleccionados `Ink`, nivel maximo 10 y precio creciente.
6. Las skins pueden comprarse y equiparse en perfil; el estado visual `Buyed`/`Selected` queda preparado en la UI.

Pendiente:

1. Probar en Play Mode todas las transacciones y la persistencia entre reinicios.
2. Aplicacion visual de la skin equipada sobre el prefab del jugador.
3. Feedback visual/sonoro de compra fallida, compra exitosa, nivel maximo y desbloqueo pendiente.
4. Definir sprites especificos para `shopBuyedSpriteResourcePath` y `shopSelectedSpriteResourcePath` si se requiere diferenciacion visual mas fuerte que el fallback actual.

### Arquitectura recomendada

Componentes actuales/futuros:
- `PersistentPlayerProfile`: mejoras permanentes, skin activa, gadgets de run habilitados por hitos y records.
- `unlockables-catalog.json`: definiciones de `skins`, `permanentUpgrades` y `runGadgets`.
- `LocalLeaderboardRepository`: ranking local de feria.
- `OutGameShopManager`: UI y compra.
- `PermanentShopService`: validacion transaccional de saldo, nivel maximo y desbloqueos.
- `PermanentUpgradeEffectResolver`: aplica efectos permanentes a Ink-Pulse, camarones y score.
- `RunGadgetUnlockService`: habilita gadgets para la tienda in-run segun hitos de records.

Reglas:
- No mezclar esta tienda con `InGameShopManager`.
- No vender gadgets aqui. Los gadgets pertenecen a la run; fuera de la run solo se desbloquea su elegibilidad por hitos automaticos.
- No aplicar upgrades directamente desde botones sin pasar por un modelo de perfil.
- Las skins deben cambiar prefab variant, visual o override, no el controlador de gameplay.
- `ShopMenu` debe consumir `PermanentShopService`; no debe recalcular precios, metas, saldo ni nivel maximo.
- La compra debe persistir en `player-profile.json`/`player-records.json` segun corresponda.
- Las mejoras permanentes deben impactar sistemas ya conectados: Ink-Pulse, charge rate, camarones y score.

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
5. Balance de `ZonaAbisopelagica`, incluyendo oscuridad, patrones y audio.
6. Validaciones automaticas para boundaries, prefabs criticos y escenas.
7. Persistencia fuera de runtime para camarones y perfil; settings queda como almacenamiento separado.

## Orden recomendado

Para la semana de feria, el orden recomendado es:

1. Auditoria corta de build, escenas, referencias y errores visibles.
2. `ShopMenu` out-of-game con mejoras permanentes reales.
3. Validacion de comics criticos ya conectados.
4. Servidor feria MVP externo con SQLite y leaderboard web.
5. Adaptador Unity minimo para crear/recuperar/sincronizar/checkout.
6. Visual minimo del segundo boss y prueba de `ZonaAbisopelagica`.
7. QA de 3 PCs en LAN real.
8. Freeze de build.

Para post-feria, el orden estructural vuelve a ser:

1. Tutorial completo con UI y spawns dirigidos sobre `TutorialDirector`.
2. Aplicacion visual de skins sobre `BabySquid`.
3. Modelo compartido de settings si aun quedan inconsistencias.
4. Opciones in-game reducidas desde pausa si no quedaron cerradas.
5. Portales con transicion formal.
6. Expansion de enemigos, bosses y zonas.

## Invariante de boundaries

Esta regla aplica a `ZonaEpipelagica`, `ZonaAbisopelagica`, `ZonaTutorial` y cualquier zona futura.

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
