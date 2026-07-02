# QATester

## Proposito

Este documento enumera parametros ajustables, criterios de prueba y contratos de validacion para la entrega. La regla de trabajo es distinguir entre:

- Parametros de balance: valores que el tester puede variar para evaluar dificultad, ritmo, recompensa o lectura visual.
- Referencias tecnicas: campos que conectan objetos de escena, prefabs, camaras, colliders o textos. No deberian modificarse durante balance salvo que se este corrigiendo cableado.

Los cambios deben probarse de uno en uno cuando sea posible. Si se modifican varios parametros al mismo tiempo, anotar el conjunto exacto porque el efecto observado ya no puede atribuirse a una sola causa.

## Metodo recomendado

1. Definir una hipotesis breve: por ejemplo, "la tienda aparece demasiado tarde" o "el Pez Globo presiona demasiado".
2. Cambiar un solo parametro.
3. Ejecutar una run corta y observar el efecto.
4. Registrar valor anterior, valor nuevo y resultado.
5. Repetir hasta encontrar un rango aceptable.

Antes de una sesion de QA, revisar el contrato de escena: perfiles de spawn, prefabs obligatorios, tags, layers, boundaries, `CleanUp`, `GameUIRoot` y reglas por zona. Si falla una referencia estructural, primero se corrige ese contrato; despues se balancean parametros.

## Estado de QA local

El loop principal de entrega cubre gameplay, botones, pausa, Game Over, skins, tienda in-run y tienda out-of-run. La validacion final debe confirmar build Windows fuera del Editor, prueba larga de `ZonaAbisopelagica` y estabilidad de persistencia.

## Contrato no balanceable: boundaries

Los limites verticales no son parametros de QA. Son infraestructura de escena.

Toda zona jugable debe contener exactamente estos nodos:

| Dominio | Contenedor | Hijos obligatorios |
| --- | --- | --- |
| Jugador | `PlayerBoundaries` | `TopBoundary`, `BottomBoundary` |
| Camara | `CameraBoundaries` | `TopBoundary`, `BottomBoundary` |

Ambos contenedores deben vivir bajo una instancia de `Assets/Content/Prefabs/World/Boundaries.prefab`. Para QA, la geometria de cada zona se corrige moviendo o redimensionando los colliders de esa instancia, no creando una jerarquia nueva ni desempaquetando el prefab.

Los sistemas leen los bordes fisicos internos de esos colliders mediante `BoundaryReferenceResolver`. Por lo tanto:

- No existe `fallbackMinY` / `fallbackMaxY` en `LevelSpawner`.
- No existe `minY` / `maxY` en `PlayerMovement`.
- No existe `topBorderOffset` en `CameraController`.
- No se asignan `topBorder` ni `bottomBorder` por Inspector.
- Si falta una jerarquia obligatoria, se corrige la escena antes de balancear.

## Contrato no balanceable: player prefab

El jugador canonico es `Assets/Content/Prefabs/Player/BabySquid.prefab`.

Reglas de QA:
- `ZonaEpipelagica`, `ZonaAbisopelagica` y `ZonaTutorial` deben contener una instancia llamada `Squid`, no una copia manual.
- En escenas, `Squid` debe mostrar referencias asignadas para sesion, progresion, camara y HUD de Ink-Pulse.
- En el asset `BabySquid.prefab`, esas referencias externas deben permanecer vacias; solo se guardan referencias internas del prefab.
- Cambios de collider, `GrazeZone`, `SquidVisual`, `InkPulseVisual`, `PortalVisual`, inventario o scripts del jugador se validan contra el prefab.
- `ZonaAbisopelagica` puede tener `LightGrazeSource` como override de instancia; eso no debe copiarse al prefab base.
- Si se ajusta la escala o posicion visual del jugador, probar minimo movimiento, graze, Ink-Pulse, compra de gadget, portal y Game Over en una run corta.

## Progresion de dificultad

Script: `RunProgressionDirector`

Nodo esperado: `GameSession`

Parametros ajustables:

| Campo | Que controla | Efecto esperado al subirlo |
| --- | --- | --- |
| `secondsToMaxIntensity` | Tiempo necesario para llegar a intensidad maxima dentro del ciclo. | La dificultad escala mas lento. |
| `postBossIntensityFloor` | Piso minimo defensivo tras superar un boss. | Normalmente no domina, porque tras Carnage la intensidad se mantiene al maximo si no se cruza portal. |
| `minScrollSpeed` | Velocidad horizontal minima del calamar. | La partida empieza mas rapida. |
| `maxScrollSpeed` | Limite asintotico de velocidad horizontal del calamar. | El maximo teorico de velocidad aumenta. |
| `speedGrowthTimeConstantSeconds` | Tiempo caracteristico de crecimiento asintotico de velocidad. | La velocidad tarda mas en acercarse al maximo si se sube. |
| `maxSpawnInterval` | Intervalo de spawn cuando la intensidad es baja. | Aparecen menos objetos al inicio. |
| `minSpawnInterval` | Intervalo de spawn cuando la intensidad es alta. | El late game respira mas si se sube; se satura mas si se baja. |
| `bossActiveSpawnIntervalMultiplier` | Multiplicador del intervalo durante boss activo. | Si sube, aparecen menos obstaculos durante boss; si baja, aparecen mas. |
| `postBossSpawnIntervalMultiplier` | Multiplicador del intervalo durante la ventana de portal post-boss. | Si sube, la ventana post-boss respira mas; por defecto debe quedar cerca de `1` para mantener intensidad. |
| `maxBossInterval` | Tiempo maximo hasta boss en baja intensidad. | El primer boss puede tardar mas. |
| `minBossInterval` | Tiempo minimo hasta boss en alta intensidad. | Los bosses aparecen menos seguido si se sube. |
| `postBossWindowSeconds` | Duracion de la oportunidad de portal tras resolver boss. | Si se sube, el jugador tiene mas tiempo para decidir cruzar. |
| `scorePerSecond` | Puntaje base ganado por segundo de gameplay activo. | El marcador sube mas rapido. |
| `scoreIntensityBonusMultiplier` | Bono proporcional a intensidad actual. | El marcador acelera mas en momentos intensos. |

Valores globales actuales para las tres zonas jugables:
- `secondsToMaxIntensity`: `150`
- `maxScrollSpeed`: `15`
- `speedGrowthTimeConstantSeconds`: `120`
- `scorePerSecond`: `3200`

## Score

Scripts: `RunProgressionDirector`, `RuntimeRunScore`, `ScoreCounterDisplay`, `GameOverMenuManager`

Nodos esperados: `GameSession` y objeto UI `Score` si existe en la escena.

Reglas vigentes:
- El score crece durante `GameSessionState.Playing`.
- Persiste al cruzar portales.
- Debe conservar el valor visible al pasar desde `ZonaEpipelagica` a `ZonaAbisopelagica`.
- Deja de crecer durante `RunEventState.Transitioning`, es decir, desde el contacto con portal hasta la carga de escena.
- Al entrar en `GameSessionState.GameOver`, `RuntimeRunScore` captura `LastCompletedScore`, `PersistentPlayerProfile` actualiza `bestScore` y luego el score runtime se reinicia.
- Al pulsar `Reintentar`, la escena cargada debe ser `ZonaEpipelagica`, incluso si la derrota ocurrio en `ZonaAbisopelagica`.
- `ScoreCounterDisplay` solo presenta el numero; no calcula progresion.
- `GameOverMenuManager` muestra `LastCompletedScore` en `PuntajeObtenido` y `PersistentPlayerProfile.BestScore` en `MaximoPuntaje`.
- La tienda temporal usa `RuntimeRunScore.TotalScore` para calcular precios.

## Velocidad del calamar

Scripts: `RunProgressionDirector`, `RuntimePlayerPace`, `PlayerMovement`

Reglas vigentes:
- La velocidad horizontal normal ya no depende de la intensidad de spawn.
- `RuntimePlayerPace` acumula tiempo efectivo de run y persiste entre portales.
- La curva es asintotica: parte en `minScrollSpeed` y se acerca lentamente a `maxScrollSpeed`.
- La velocidad deja de acumular durante `RunEventState.Transitioning`.
- `GameOver` reinicia la progresion de velocidad.
- Ink-Pulse sigue usando `inkPulseHorizontalSpeed` como override temporal.

La intensidad de spawn sigue siendo otra curva: baja a alta, boss, post-boss intenso; si el jugador no cruza portal se mantiene alta, y si cruza portal la zona destino reinicia esa intensidad.

## Spawn general

Scripts: `LevelSpawner`, `ZoneSpawnProfile`, `EnemySpawnSelector`, `SpawnPositionResolver`, `SpawnedObjectConfigurator`

Nodo esperado: `LevelSpawner`

Fuente de parametros:
- Balancear siempre el asset `ZoneSpawnProfile` asignado al `LevelSpawner` de la zona.
- Si `zoneSpawnProfile` esta vacio, la escena esta mal configurada y debe corregirse antes de QA.
- `EnemySpawnSelector`, `SpawnPositionResolver` y `SpawnedObjectConfigurator` no tienen parametros de QA. Son servicios internos: seleccionan perfiles, calculan posiciones y aplican tag/layer/contexto a lo que instancia `LevelSpawner`.

Assets actuales:
- `Assets/Implementation/Config/Spawning/ZonaEpipelagicaSpawnProfile.asset`
- `Assets/Implementation/Config/Spawning/ZonaAbisopelagicaSpawnProfile.asset`
- `Assets/Implementation/Config/Spawning/ZonaTutorialSpawnProfile.asset`

Parametros ajustables:

| Campo | Fuente | Que controla | Efecto esperado al subirlo |
| --- | --- | --- |
| `timeBetweenSpawns` | `ZoneSpawnProfile` | Intervalo base cuando no hay `RunProgressionDirector`. | Menos spawns si no hay progresion conectada. |
| `spawnDistanceFromCameraRight` | `ZoneSpawnProfile` | Distancia horizontal desde el borde derecho de camara. | Los objetos aparecen mas lejos y tardan mas en llegar. |
| `verticalPadding` | `ZoneSpawnProfile` | Margen vertical contra camara y boundaries. | Los spawns se alejan mas de los limites. |
| `coinSpawnChance` | `ZoneSpawnProfile` | Probabilidad de que el spawn sea camaron. | Hay mas recompensa y menos peligro. |
| `rareCoinSpawnChanceWithinCoins` | `ZoneSpawnProfile` | Probabilidad de camaron x10 cuando ya salio camaron. | Aumenta la economia de la run. |
| `fishingRodEnemyInterval` | `ZoneSpawnProfile` | Cada cuantos enemigos de juego normal se fuerza una cana. | La cana aparece menos seguido si se sube. |
| `upperZoneSpawnCoverage` | `ZoneSpawnProfile` | Porcion superior disponible para Pez Globo. En Epi/Abiso vale `0.8`. | El Pez Globo tiene mas dispersion vertical dentro del semicampo superior. |
| `lowerZoneSpawnCoverage` | `ZoneSpawnProfile` | Porcion inferior disponible para enemigos sin regla vertical propia. | Da mas dispersion a amenazas que usan el fallback inferior. |

`enemyProfiles` define el peso por enemigo dentro del `ZoneSpawnProfile`:

| Campo | Que controla | Nota de test |
| --- | --- | --- |
| `prefab` | Prefab instanciado. | Debe tener tag esperado y collider correcto. |
| `enemyTag` | Identidad logica del enemigo. | Usar `EnemyMina`, `EnemyPezGlobo`, `EnemyCanaPescar`, `EnemyRay` o `EnemyJellyfish`. |
| `baseWeight` | Peso relativo de aparicion. | Mas alto implica mayor frecuencia relativa. |
| `minIntensity` | Intensidad minima para poder aparecer. | Sirve para retrasar enemigos complejos. |
| `spawnIntervalMultiplier` | Modificador del intervalo despues de ese enemigo. | Mas alto deja mas aire tras ese spawn. |

Valor vigente de referencia: `coinSpawnChance = 0.225`, equivalente a tres cuartos del valor anterior `0.3`.

## Tienda temporal

Scripts: `LevelSpawner`, `ZoneSpawnProfile`, `DealerFish`, `InGameShopManager`, `ShopOfferSelector`, `ShopPriceCalculator`, `RunGadgetUnlockService`

Nodos esperados: `LevelSpawner`, prefab `DealerFish`, `UI/InGameShopManager`

Parametros ajustables:

| Campo | Fuente | Que controla |
| --- | --- | --- |
| `enableDealerFishSpawns` | `ZoneSpawnProfile` | Activa o desactiva aparicion de tienda. |
| `firstDealerFishSpawnDelay` | `ZoneSpawnProfile` | Tiempo base hasta el primer DealerFish. |
| `dealerFishSpawnInterval` | `ZoneSpawnProfile` | Intervalo base entre DealerFish posteriores. |
| `dealerFishIntervalRandomMultiplierMin` | `ZoneSpawnProfile` | Multiplicador aleatorio minimo del intervalo base. |
| `dealerFishIntervalRandomMultiplierMax` | `ZoneSpawnProfile` | Multiplicador aleatorio maximo del intervalo base. |
| `dealerFishSpawnDistanceFromCameraRight` | `ZoneSpawnProfile` | Distancia horizontal propia del DealerFish desde el borde derecho de camara. |
| `dealerFishSpawnZoneMin` | `ZoneSpawnProfile` | Inicio normalizado de la zona vertical de aparicion, dentro de la mitad inferior. |
| `dealerFishSpawnZoneMax` | `ZoneSpawnProfile` | Fin normalizado de la zona vertical de aparicion, limitado a la mitad inferior. |
| `offerDurationSeconds` | `InGameShopManager` | Tiempo disponible para comprar. |
| `pauseGameplayWhileOpen` | `InGameShopManager` | Si la tienda congela gameplay mientras corre en tiempo real. |
| `globalPriceMultiplier` | `InGameShopManager` | Multiplicador general de precios. |
| `scorePriceStep` | `InGameShopManager` | Cada cuantos puntos aumenta el multiplicador lineal de progreso. Por defecto la formula usa `score / 100000 + 1`. |
| `randomPriceMultiplierMin` | `InGameShopManager` | Minimo del multiplicador aleatorio de oferta. |
| `randomPriceMultiplierMax` | `InGameShopManager` | Maximo del multiplicador aleatorio de oferta. |
| `offers[].basePriceOverride` | `InGameShopManager` | Precio base alternativo para una oferta concreta. |
| `textPulseAmplitude` | `InGameShopManager` | Magnitud de pulso para `B` y `Precio`. |
| `textPulseFrequency` | `InGameShopManager` | Velocidad del pulso visual de tienda. |

Reglas vigentes:

- Comprar usa tecla `B` o click sobre el boton `Comprar`.
- `SinSaldo` aparece solo despues de intentar comprar con `B` o click sin camarones suficientes.
- La tienda puede ofrecer un gadget repetido, pero no permite comprarlo si ya existe en inventario.
- La tienda solo puede ofrecer gadgets habilitados por `RunGadgetUnlockService`.
- Formula de precio vigente: `ceil(((score / scorePriceStep) + 1) * randomPriceMultiplier * precioBaseMinimo * globalPriceMultiplier)`.
- `ShopOfferSelector` y `ShopPriceCalculator` son helpers puros sin parametros de Inspector. El balance se hace en `InGameShopManager` y en cada `GadgetShopItem`.
- Al colisionar con `DealerFish`, el objeto intenta abrir tienda una vez y permanece visible; conserva su collider trigger para que `DestroyOffscreen` pueda limpiarlo cuando queda atras. La proteccion contra reapertura vive en su flag interno de consumo.
- El tiempo real entre apariciones es `intervaloBase * random(1, 3)` con los limites configurables anteriores.
- Por contrato actual, `DealerFish` aparece entre `0` y `0.25` del rango vertical de `PlayerBoundaries`; `0` es `BottomBoundary` y `0.5` es el centro del rango.

## Portales

Script de contacto: `ScenePortal`

Prefab: `Assets/Content/Prefabs/Portals/ScenePortal.prefab`

Script de aparicion: `LevelSpawner`

Script de rutas: `SceneFlowController`

Parametros ajustables:

| Campo | Que controla | Nota de test |
| --- | --- | --- |
| `portalPrefab` | Prefab de portal que se instancia en runtime. | Vive en `ZoneSpawnProfile`. |
| `portalSpawnPolicy` | Regla de aparicion del portal. | Las zonas jugables usan `PostBossWindow`; `ZonaTutorial` usa `Disabled` porque su portal es dirigido por `TutorialDirector`. Vive en `ZoneSpawnProfile`. |
| `portalSpawnedParent` | Contenedor donde se agrupan los portales instanciados. | Debe apuntar al nodo `Portals`. |
| `firstPortalSpawnDelay` | Espera antes de la tirada post-boss. | Vive en `ZoneSpawnProfile`; las zonas jugables usan `3s`. |
| `postBossPortalSpawnChance` | Probabilidad de que aparezca portal tras el delay post-boss. | Vive en `ZoneSpawnProfile`; `1` significa garantizado y valores menores introducen probabilidad real. |
| `requireNoActivePortal` | Evita crear otro portal si uno anterior sigue vivo. | Vive en `ZoneSpawnProfile`; debe estar activo por defecto. |
| `fallbackTransitionDelay` | Espera de respaldo si el jugador no tiene `PlayerVisualStateController`. | Vive en `ScenePortal`; normalmente debe ganar la duracion de `PortalEffect`. |
| `primaryGameplaySceneName` | Zona base o retorno. | Vive en `SceneFlowController`; por defecto `ZonaEpipelagica`. |
| `secondaryGameplaySceneName` | Zona alterna. | Vive en `SceneFlowController`; por defecto `ZonaAbisopelagica`. |

Reglas vigentes:

- El portal usa tag `Portal`, no `Shrimp` ni `Collectible`.
- El portal usa capa `Collectible` para participar en colisiones de mundo.
- `ZonaAbisopelagica` debe estar habilitada en Build Settings.
- Cruzar un portal conserva gadgets e Ink-Pulse.
- Al tocar portal, antes del cambio de escena debe verse solo `PortalVisual`; `SquidVisual` e `InkPulseVisual` quedan ocultos.
- `PortalEffect.anim` debe reproducirse una vez y no tener loop.
- Entrar en Game Over reinicia gadgets e Ink-Pulse.

## Iluminacion de ZonaAbisopelagica

Script: `ZoneLightingController`

Nodo esperado: `EnviromentRoot_ZonaAbisopelagica/ZoneLightingController` en `ZonaAbisopelagica`

Parametros ajustables:

| Campo | Que controla | Efecto esperado al subirlo |
| --- | --- | --- |
| `blackAlpha` | Opacidad fija de `LayerBlack`. | `ZonaAbisopelagica` se ve mas oscura. |
| `overlayPadding` | Margen extra de cobertura respecto a la camara. | Evita bordes sin overlay en aspect ratios amplios. |
| `maskSortingOrderPadding` | Rango de sorting usado por el fallback con `SpriteMask`. | Solo afecta si `useCompositeLightOverlay` esta desactivado. |
| `lightHoleRadius` | Radio de mundo de cada zona revelada. | Cada entidad revela un area mayor. |
| `lightEdgeSoftness` | Porcion del radio que se usa como borde gradual. | El borde del circulo se vuelve mas suave; si sube demasiado, reduce el centro completamente claro. |
| `maskAlphaCutoff` | Umbral alfa de la mascara circular fallback. | Solo afecta si `useCompositeLightOverlay` esta desactivado. |
| `useCompositeLightOverlay` | Usa una unica textura runtime para componer todas las luces. | Evita acumulacion visual extrana cuando dos luces se cruzan. |
| `compositeTextureWidth` | Resolucion horizontal de la textura de oscuridad. | Mayor nitidez horizontal, mayor costo por frame. |
| `compositeTextureHeight` | Resolucion vertical de la textura de oscuridad. | Mayor nitidez vertical, mayor costo por frame. |
| `compositeUpdatesPerSecond` | Frecuencia maxima de recomposicion del overlay. | El contrato actual usa `60` para evitar saltos visibles durante Ink-Pulse. |

Reglas vigentes:

- `LightGraze` es visual: no carga Ink-Pulse y no reemplaza `GrazeDetector`.
- En modo compuesto, `LayerBlack` usa una textura generada y `maskInteraction = None`.
- `VisibleOutsideMask` solo corresponde al modo fallback con `SpriteMask`.
- La instancia `Squid` de `BabySquid.prefab` tiene `LightGrazeSource` en `ZonaAbisopelagica`.
- `SpawnedObjectConfigurator`, invocado por `LevelSpawner`, agrega `LightGrazeSource` a entidades runtime solo si existe `ZoneLightingController`.
- `LightGrazeSource` puede declarar ancla, escala X/Y de luz y titileo; esos campos son lectura visual, no dificultad mecanica.
- `DealerFish_ZonaAbisopelagica` debe revelar su soporte visual/roca y `UnknownBoss` debe tener luz propia titilante.
- `SSCarnage` y `BossNetWall` no participan porque no aparecen en `ZonaAbisopelagica`.
- `ZonaAbisopelagicaSpawnProfile` debe usar `DealerFish_ZonaAbisopelagica.prefab`; tutorial y zona epipelagica deben conservar `DealerFish.prefab`.

## Gadgets e inventario

Scripts: `GadgetId`, `GadgetActivationKind`, `GadgetCatalog`, `GadgetShopItem`, `PlayerGadgetInventory`, `GadgetInventoryHud`, `RunGadgetUnlockService`

Parametros ajustables:

| Campo | Script | Que controla |
| --- | --- | --- |
| `GadgetCatalog.GetBaseShopPrice()` | `GadgetCatalog` | Precio base por tipo de gadget. Actualmente es codigo, no Inspector. |
| `gadgetId` | `GadgetShopItem` | Identidad del gadget vendido por el prefab. |
| `hudIcon` | `GadgetShopItem` | Icono mostrado en tienda y HUD. |
| `hudIconTint` | `GadgetShopItem` | Tinte del icono. |
| `grantStartingInventory` | `PlayerGadgetInventory` | Permite iniciar con gadgets para pruebas. |
| `startWithShellShield` | `PlayerGadgetInventory` | Da Shell Shield inicial si `grantStartingInventory` esta activo. |
| `startWithInkBottle` | `PlayerGadgetInventory` | Da Ink-Bottle inicial si `grantStartingInventory` esta activo. |
| `firstSlotKey` | `GadgetInventoryHud` | Etiqueta visual del primer slot. Debe quedar `Q`. |
| `secondSlotKey` | `GadgetInventoryHud` | Etiqueta visual del segundo slot. Debe quedar `W`. |
| `textPulseAmplitude` | `GadgetInventoryHud` | Magnitud de pulso para letras de slot activo. |
| `textPulseFrequency` | `GadgetInventoryHud` | Velocidad del pulso de letras de slot activo. |

Regla vigente de input:

- `Gadget1` se activa con `Q` si contiene un gadget activo.
- `Gadget2` se activa con `W` si contiene un gadget activo.
- `Shell Shield` es pasivo y no muestra tecla.
- `Ink-Bottle` es activo y fuerza `InkPulseState.Ready` si el Ink-Pulse puede recibir ese cambio.
- Al usar `Ink-Bottle` correctamente, el gadget se consume, libera su slot y desaparece del HUD. Si `TryForceReady()` falla, debe conservarse.
- Los gadgets y slots persisten al cruzar portales.
- Los gadgets y slots se reinician al entrar en Game Over.
- Los gadgets no se compran en `ShopMenu`; solo se compran durante la run desde `DealerFish`.
- `player-profile.json/runGadgetUnlocks` no representa posesion runtime. Representa elegibilidad permanente para aparecer en la tienda in-game.

## Ink-Pulse

Script: `InkPulseController`

Nodo esperado: `Squid`

Parametros ajustables:

| Campo | Que controla | Efecto esperado al subirlo |
| --- | --- | --- |
| `chargeRate` | Carga ganada por proximidad/graze. | El recurso llega a `Ready` mas rapido. |
| `maxCharge` | Carga necesaria para estar listo. | Requiere mas riesgo antes de activar. |
| `currentCharge` | Carga inicial/debug serializada. | Sirve para pruebas puntuales, no para balance final. |
| `pulseDuration` | Duracion del estado `Active`. | El pulso dura mas. |

Reglas vigentes:

- Se activa con click izquierdo o tecla `Space`.
- La carga del Ink-Pulse persiste al cruzar portales.
- Si el Ink-Pulse esta en `Active` al cruzar, persiste con su tiempo restante.
- Durante `Active`, `InkBar` debe vaciarse progresivamente segun `PulseRemainingSeconds / PulseDuration`; no debe saltar de lleno a vacio al iniciar el pulso.
- El texto `CLICK` debe ocultarse durante `Active`, aunque la barra empiece visualmente llena en el primer instante del consumo.
- La carga vuelve a cero al entrar en Game Over.
- No puede activarse mientras `InGameShopManager` esta en `ShopEventState.Offering`.

## Musica dinamica del Ink-Pulse

Script: `InkPulseMusicCrossfader`

Nodo esperado: `Soundtrack` en `ZonaEpipelagica`

Parametros ajustables:

| Campo | Que controla | Efecto esperado al subirlo |
| --- | --- | --- |
| `normalTargetVolume` | Volumen maximo de la pista normal. | La base musical se oye mas fuerte fuera del pulso. |
| `inkTargetVolume` | Volumen maximo de la pista `INK`. | La version intensa se oye mas fuerte durante el pulso. |
| `fadeSeconds` | Duracion del cruce entre ambas pistas. | La transicion se vuelve mas gradual. |
| `useEqualPowerCrossfade` | Tipo de curva de mezcla. | Puede mantener mas energia en el centro del cruce, pero no es el valor recomendado para dos mezclas completas. |
| `syncStartDelay` | Margen antes de iniciar ambas pistas con `PlayScheduled`. | Da mas holgura al motor de audio para iniciar las pistas alineadas. |

Reglas vigentes:

- Las dos pistas deben sonar sincronizadas desde el mismo tiempo DSP.
- En reposo, la pista normal queda al volumen objetivo y la pista `INK` queda en cero.
- Durante `InkPulseState.Active`, la pista normal cruza hacia cero y la pista `INK` cruza hacia su volumen objetivo.
- Para dos mezclas completas del mismo tema, mantener `useEqualPowerCrossfade` desactivado.
- Si se percibe desfase, revisar la exportacion de los audios antes de tocar parametros: mismo inicio, tempo, duracion y loop.

## Pitch progresivo del soundtrack

Script: `SoundtrackPitchProgression`

Nodos esperados:
- `AudioRoot_ZonaEpipelagica/Soundtrack`
- `AudioRoot_ZonaAbisopelagica/Soundtrack`

Parametros ajustables:

| Campo | Que controla | Efecto esperado al subirlo |
| --- | --- | --- |
| `tracks` | AudioSources afectados por el pitch progresivo. | Permite incluir varias pistas sincronizadas, como normal e `INK`. |
| `pitchIncreasePerSecond` | Constante lineal de crecimiento por segundo efectivo de run. | La musica acelera antes durante la progresion. |
| `maxPitchOffset` | Limite del incremento sobre el pitch base. | Permite un pico mayor de intensidad sonora. |

Reglas vigentes:

- Usa `RuntimePlayerPace.ElapsedSpeedSeconds`, no tiempo absoluto de escena.
- No debe avanzar en pausa, Game Over ni transiciones donde la progresion esta bloqueada.
- En `ZonaEpipelagica`, `tracks` debe incluir las dos pistas del crossfade para mantenerlas alineadas.
- En `ZonaAbisopelagica`, `tracks` debe incluir la pista unica del soundtrack.
- Valores actuales: `pitchIncreasePerSecond = 0.0005`, `maxPitchOffset = 0.18`.
- Con pitch base `1.1`, el maximo esperado es `1.28`.

Pruebas recomendadas:

- Iniciar run y observar que el pitch sube gradualmente.
- Pausar y confirmar que el pitch deja de crecer.
- Activar Ink-Pulse en `ZonaEpipelagica` y confirmar que el crossfade no revela desafinacion entre pistas.
- Entrar a Game Over/reintentar y confirmar que la siguiente run vuelve al pitch base.

## Movimiento del jugador

Script: `PlayerMovement`

Nodo esperado: `Squid`

Parametros ajustables:

| Campo | Que controla | Efecto esperado al subirlo |
| --- | --- | --- |
| `normalHorizontalSpeed` | Velocidad horizontal base si no hay progresion. | El avance normal se acelera. |
| `inkPulseHorizontalSpeed` | Velocidad horizontal durante Ink-Pulse. | El pulso empuja mas fuerte hacia adelante. |
| `normalVerticalSpeed` | Respuesta vertical normal al mouse. | El jugador corrige altura mas rapido. |
| `inkPulseVerticalSpeed` | Respuesta vertical durante Ink-Pulse. | El jugador maniobra mas durante el pulso. |
| `randomizeInitialYWithinPlayerBoundaries` | Si el squid inicia con Y aleatoria dentro de `PlayerBoundaries`. | Desactivarlo sirve para pruebas deterministicas de escena. |
| `smoothSpeedTransition` | Suavizado entre velocidades. | Transiciones mas rapidas si se sube. |
| `baseRotationZ` | Rotacion base visual. | Cambia la orientacion del squid. |
| `maxTiltAngle` | Inclinacion maxima por movimiento vertical. | La animacion de giro se nota mas. |
| `tiltSmoothSpeed` | Suavizado del tilt. | El giro responde mas rapido. |

## Enemigos actuales

### Pez Globo

Script de comportamiento: `PufferfishEnemy`

Owner de parametros: `ZoneSpawnProfile.pufferfishTuning`.

Parametros ajustables:

| Campo | Que controla | Efecto esperado al subirlo |
| --- | --- | --- |
| `fallSpeed` | Velocidad vertical base. | Se mueve mas rapido en la direccion actual. |
| `expandedSpeedMultiplier` | Multiplicador de velocidad al expandirse. | Se mueve mas rapido durante amenaza, hacia arriba o hacia abajo segun su direccion actual. |
| `proximityRadius` | Distancia para expandirse. | Se activa desde mas lejos. |
| `expandedScaleMultiplier` | Escala objetivo al expandirse. | Ocupa mas espacio. |
| `expansionSmoothSpeed` | Velocidad de interpolacion de escala. | La expansion se ve mas inmediata. |
| `erraticDirectionChangeIntervalMin` | Tiempo minimo antes de poder cambiar direccion vertical. | Cambia de direccion con mas frecuencia si baja. |
| `erraticDirectionChangeIntervalMax` | Tiempo maximo antes de evaluar cambio de direccion vertical. | Cambia de direccion con menos frecuencia si sube. |
| `erraticDirectionChangeChance` | Probabilidad de invertir direccion en cada evaluacion. | El movimiento se vuelve mas erratico. |

Estos campos no se ajustan en el prefab `PezGlobo`.

El prefab `PezGlobo` debe tener un unico `CircleCollider2D` en la raiz. La expansion escala el `Transform`, por lo que el collider circular acompana el crecimiento visual y fisico.
La animacion de hinchado se reproduce una sola vez al entrar en expansion. El clip `PezGlobo.anim` debe quedar sin loop, y el enemigo no vuelve a deshincharse. Al hincharse no fuerza subida: conserva la direccion vertical actual y solo aumenta velocidad.

### Mina

La mina no requiere script propio en esta entrega. Su balance depende de:

- Perfil `EnemyMina` en `ZoneSpawnProfile.enemyProfiles`.
- Regla de posicion global de `SpawnPositionResolver`: puede aparecer en todo `PlayerBoundaries`.
- Collider y escala del prefab.

### Ray

Estado de entrega: implementado y no habilitado por balance con `baseWeight: 0` dentro de `ZonaEpipelagicaSpawnProfile`. Para ensayos controlados, subir ese peso de forma local y restaurarlo despues de la prueba.

Script de comportamiento: `RayEnemy`

Owner de parametros: `ZoneSpawnProfile.rayTuning`.

Reglas vigentes:
- Solo esta en `ZonaEpipelagicaSpawnProfile`.
- Aparece en los tres cuartos inferiores del rango jugable.
- Se mueve en diagonal hacia la izquierda.
- Alterna por spawn entre diagonal ascendente y descendente.
- El prefab de entrega es base: square visible, `BoxCollider2D` trigger y layer `Enemy`.

Parametros ajustables:

| Campo | Que controla | Efecto esperado al subirlo |
| --- | --- | --- |
| `horizontalSpeed` | Velocidad propia hacia la izquierda. | Llega antes al jugador y sale antes del plano. |
| `verticalSpeed` | Componente vertical de la diagonal. | Cruza mas rapido entre carriles altos y bajos. |

### Jellyfish

Estado de entrega: implementado y no habilitado por balance con `baseWeight: 0` dentro de `ZonaAbisopelagicaSpawnProfile`. Para ensayos controlados, subir ese peso de forma local y restaurarlo despues de la prueba.

Script de comportamiento: `JellyfishEnemy`

Owner de parametros: `ZoneSpawnProfile.jellyfishTuning`.

Reglas vigentes:
- Solo esta en `ZonaAbisopelagicaSpawnProfile`.
- Aparece en todo el rango jugable.
- Se mueve siempre hacia arriba lentamente.
- En abisal recibe `LightGrazeSource` por `SpawnedObjectConfigurator`.
- El prefab de entrega es base: square visible, `BoxCollider2D` trigger y layer `Enemy`.

Parametros ajustables:

| Campo | Que controla | Efecto esperado al subirlo |
| --- | --- | --- |
| `upwardSpeed` | Velocidad vertical ascendente. | La medusa abandona carriles bajos mas rapido. |

### Cana de pescar

Script de comportamiento: `FishingRodEnemy`

Owner de parametros: `ZoneSpawnProfile.fishingRodTuning`.

Parametros ajustables:

| Campo | Que controla | Efecto esperado al subirlo |
| --- | --- | --- |
| `dropSpeed` | Velocidad vertical de bajada hacia la Y capturada del jugador. | La cana cae mas brusca y rapidamente. |
| `startYOffsetBelowTopBoundary` | Distancia bajo el `TopBoundary` desde donde empieza la bajada. | La cana nace mas abajo si se sube. |
| `descentStartViewportX` | Punto horizontal de viewport donde se permite iniciar la bajada. `1` es el borde derecho de camara. | Valores mayores inician la accion levemente antes de entrar a camara; valores menores la retrasan. |
| `descentWindupSeconds` | Pausa breve entre entrar en ventana de lectura y empezar a caer. | Da mas lectura, pero debe mantenerse corta para no volver trivial la amenaza. |
| `enableFastPaceHorizontalHold` | Habilita el anclaje horizontal cuando la partida ya va rapida. | Evita que la cana quede atras durante la bajada en late game. |
| `horizontalHoldMinScrollSpeed` | Velocidad minima para activar el anclaje horizontal. | Si sube, el anclaje aparece mas tarde; si baja, aparece antes. |
| `horizontalHoldViewportX` | X de viewport donde la cana se mantiene mientras espera, anticipa y baja a alta velocidad. | Valores mayores la mantienen mas a la derecha. |
| `arriveDistance` | Tolerancia para considerar que llego a la Y objetivo. | Detiene el movimiento con menos precision si se sube. |
| `horizontalLeadTimePaddingSeconds` | Margen temporal agregado al calculo de distancia horizontal del anzuelo. | El anzuelo aparece mas lejos cuando el jugador va rapido. |
| `minimumHorizontalLeadDistance` | Distancia minima propia de la cana desde el borde derecho de camara. | Evita que aparezca demasiado cerca a velocidades bajas. |

Tambien depende de:
- Perfil `EnemyCanaPescar` en `ZoneSpawnProfile.enemyProfiles`.
- `fishingRodEnemyInterval`.
- Collider y escala del prefab.

Reglas vigentes:
- La cana regular captura la altura Y del jugador al spawnear.
- Luego espera a entrar en ventana de lectura, mantiene una pausa breve y baja verticalmente desde el top del rango jugable hasta esa Y.
- Si la velocidad horizontal supera `horizontalHoldMinScrollSpeed`, puede mantener su X en `horizontalHoldViewportX` hasta terminar la bajada.
- La distancia X de aparicion se calcula con la velocidad horizontal actual del jugador y el tiempo estimado de caida.
- No persigue al jugador despues de capturar la Y.
- La cana regular se fuerza solo fuera de `BossActive`.
- Cualquier anzuelo adicional del SS Carnage debe probarse como prefab/ataque de boss independiente, no como excepcion del spawner regular.

Contrato de tamano visual de `CanaPescar`:
- El tamano de `Rope`/`Visual` en `CanaPescar.prefab` es parte de la autoria del prefab. No es legacy ni debe reducirse por normalizacion automatica.
- Al cambiar el tamano visual de `CanaPescar`, validar que el root siga con tag `EnemyCanaPescar` y layer `Enemy`, y que `Rope`/`Visual` permanezcan como hijos en layer `Enemy`.
- El cleanup debe ocurrir solo cuando los bounds agregados de colliders/renderers de la cana completa quedaron detras de la distancia segura.

## SS Carnage

Scripts: `BossEventDirector`, `SSCarnageController`, `SSCarnageNetWall`

Escena esperada actual para SS Carnage: `ZonaEpipelagica`. `ZonaAbisopelagica` puede reutilizar `BossEventDirector` como disparador generico de `UnknownBoss`/`FlappyBoss`, pero no debe tener `SSCarnageManager`, `SSCarnage` ni `BossNetWall`.

Contrato de `BossPillars`: `TopPillar` y `BottomPillar` deben conservar `PolygonCollider2D` trigger como collider jugable. No deben tener `BoxCollider2D`; si aparece un box, la colision vuelve a ser rectangular y contradice la silueta autorada.

Parametros ajustables:

| Campo | Script | Que controla |
| --- | --- | --- |
| `triggerAfterSeconds` | `BossEventDirector` | Tiempo base para intentar disparar boss si la progresion lo permite. |
| `spawnDistanceFromCameraRight` | `BossEventDirector` | Distancia inicial del Carnage desde el borde derecho de camara. |
| `viewportY` | `BossEventDirector` | Altura de aparicion relativa en camara. |
| `triggerOnce` | `BossEventDirector` | Si el evento ocurre una sola vez. |
| `wideCameraHoldSeconds` | `BossEventDirector` | Duracion de la vista amplia. |
| `wideCameraTransitionSmoothTime` | `BossEventDirector` | Suavizado del cambio de camara. |
| `wideCameraExtraTopSpace` | `BossEventDirector` | Espacio vertical extra en vista amplia. |
| `warningDuration` | `SSCarnageController` | Tiempo antes de desplegar la red. |
| `warningViewportX` | `SSCarnageController` | Posicion horizontal de aviso en viewport. |
| `verticalOffsetAbovePlayerTopBoundary` | `SSCarnageController` | Offset vertical sobre el top boundary del jugador. |
| `followSmoothTime` | `SSCarnageController` | Suavizado del seguimiento durante warning. |
| `destroyAfterNetDeploy` | `SSCarnageController` | Si el Carnage se retira/destruye tras desplegar red. |
| `destroyDelayAfterNetDeploy` | `SSCarnageController` | Espera antes de destruir tras red. |
| `exitDistanceFromCameraRight` | `SSCarnageController` | Distancia objetivo de salida hacia la derecha. |
| `exitSpeed` | `SSCarnageController` | Velocidad de salida. |
| `netSpawnDistanceFromCameraRight` | `SSCarnageController` | Distancia minima de aparicion de la red desde el borde derecho de camara. |
| `netHorizontalLeadTimeSeconds` | `SSCarnageController` | Ventaja temporal usada para separar la red segun la velocidad horizontal actual del jugador. |
| `netViewportY` | `SSCarnageController` | Altura relativa de spawn de red. |
| `deployNetOnStart` | `SSCarnageController` | Si el ataque inicia automaticamente. |

La posicion X de `BossNetWall` usa esta formula:

```text
distanciaFinal = max(netSpawnDistanceFromCameraRight, velocidadHorizontalActualDelJugador * netHorizontalLeadTimeSeconds)
```

Esto replica el criterio del anzuelo/cana: si el jugador va mas rapido, el obstaculo nace mas lejos para mantener una ventana de lectura proporcional. `netSpawnDistanceFromCameraRight` queda como piso defensivo para velocidades bajas. Los valores vigentes de referencia en el prefab `SSCarnage` son `netSpawnDistanceFromCameraRight = 4` y `netHorizontalLeadTimeSeconds = 0.75`.

`SSCarnageNetWall` ajusta altura visual y volumen de colision automaticamente desde `PlayerBoundaries`. Esa altura no se balancea desde Inspector, y la red rota queda como feedback visual fijo.

Para cleanup, `SSCarnage`, `BossNetWall` y `DealerFish` deben conservar colliders trigger activos mientras sean visibles. Si alguno queda atras, `DestroyOffscreen` lo destruye por tag/collider; no se debe resolver desactivando el visual manualmente.

## Camara y mundo

Scripts: `CameraController`, `HorizontalTracker`, `ParallaxLayer`, `DestroyOffscreen`

Parametros ajustables:

| Campo | Script | Que controla |
| --- | --- | --- |
| `offset` | `CameraController` | Desfase de camara respecto al jugador. |
| `smoothTime` | `CameraController` | Suavizado del seguimiento. |
| `returnToFollowHorizontalSmoothTime` | `CameraController` | Suavizado horizontal al volver desde vista de evento a seguimiento normal. |
| `enableInkPulseScreenPulse` | `CameraController` | Activa o desactiva el feedback visual al iniciar Ink-Pulse. |
| `inkPulseFeedbackDuration` | `CameraController` | Duracion del tambaleo/pulso de pantalla. |
| `inkPulseShakeAmplitude` | `CameraController` | Magnitud del desplazamiento breve de camara. |
| `inkPulseZoomAmplitude` | `CameraController` | Magnitud del pulso de zoom ortografico. |
| `inkPulseShakeFrequency` | `CameraController` | Frecuencia del tambaleo durante el feedback. |
| `parallaxFactor` | `ParallaxLayer` | Intensidad de desplazamiento relativo del fondo. |
| `followVertical` | `ParallaxLayer` | Si la capa acompana movimiento vertical. |
| `extraTilesPerSide` | `ParallaxLayer` | Cantidad de tiles laterales para continuidad visual. |

`HorizontalTracker` no tiene parametros de balance: solo sigue la camara asignada. `DestroyOffscreen` no tiene parametros de balance: sigue el borde izquierdo de la camara, ajusta el alto del trigger desde `CameraBoundaries` y destruye enemigos, camarones, collectibles y portales que ya salieron de pantalla. La referencia `targetCamera` es cableado tecnico, no un valor de balance.

Contrato de `CleanUp`:
- Debe ser instancia de `Assets/Content/Prefabs/World/CleanUp.prefab`.
- Su altura efectiva es la distancia interna entre `CameraBoundaries/BottomBoundary` y `CameraBoundaries/TopBoundary`.
- Para corregir su cobertura vertical, no se edita el prefab ni la posicion del `GarbageCollector`; se corrigen los colliders de `CameraBoundaries`.

Contrato de `Boundaries`:
- Debe ser instancia de `Assets/Content/Prefabs/World/Boundaries.prefab`.
- `HorizontalTracker` no requiere una referencia serializada a camara; la resuelve desde `Camera.main`.
- Cada zona puede conservar overrides de posicion y colliders, porque esos valores representan su geometria real.
- No se crean boundaries alternativos para balancear spawns, camara, red o limpieza.

## UI y menus

Scripts: `PauseMenuManager`, `GameOverMenuManager`, `OptionsMenuManager`, `MenuButtonAnimation`, `MenuBubbles`

Parametros ajustables:

| Campo | Script | Que controla |
| --- | --- | --- |
| `fadeDuration` | `PauseMenuManager` / `GameOverMenuManager` | Duracion del fundido. |
| `fadeDuration` | `OptionsMenuManager` | Duracion del fundido de opciones. |
| `zigzagOffset` | `PauseMenuManager` / `GameOverMenuManager` | Distancia inicial/final de entrada visual. |
| `zigzagDuration` | `PauseMenuManager` / `GameOverMenuManager` | Duracion de movimiento de cada elemento. |
| `zigzagDelay` | `PauseMenuManager` / `GameOverMenuManager` | Separacion temporal entre elementos animados. |
| `cantidadBurbujas` | `MenuBubbles` | Numero de burbujas decorativas. |
| `velocidad` | `MenuBubbles` | Velocidad vertical media de burbujas. |
| `tamanoMin` / `tamanoMax` | `MenuBubbles` | Rango de tamano de burbujas. |
| `colorBurbuja` | `MenuBubbles` | Color y alpha base de burbujas. |

`MenuButtonAnimation` no tiene parametros ajustables por boton. Si el pulso/hover de botones requiere balance, debe centralizarse antes en un manager/controlador de UI.

Reglas vigentes:
- La pausa se alterna con `P` o `Esc`.
- `OptionsMenu` debe abrirse desde MainMenu, ShopMenu y escenas jugables como prefab/panel, no como escena.
- El fondo `Background`/`Fondo` del prefab debe verse detras del panel y bloquear visualmente el contenido anterior sin cubrir los controles del menu.
- Cambiar volumen, resolucion o fullscreen debe persistir en `PlayerPrefs`; no debe escribir en `Application.persistentDataPath/db/`.

## Economia de camarones y perfil persistente

Scripts: `ShrimpValue`, `ShrimpRuntimeWallet`, `PersistentPlayerProfile`, `PlayerProfileRepository`, `ShrimpCounterDisplay`, `PermanentShopService`, `PermanentUpgradeEffectResolver`

Parametros ajustables:

| Campo | Script | Que controla |
| --- | --- | --- |
| `amount` | `ShrimpValue` | Valor de cada camaron recogible. |
| `prefix` | `ShrimpCounterDisplay` | Texto previo al numero en HUD. |

`ShrimpRuntimeWallet` no tiene parametros de balance en Inspector. Es almacenamiento runtime y API de suma/gasto.

Formato HUD:
- `0` a `999`: numero completo.
- `1000` a `9999`: miles con un decimal si existe (`1K`, `1.1K`, `1.2K`).
- `10000` a `999999`: miles enteros truncados (`10K`, `11K`, `15K`, `100K`).
- `1000000` a `9999999`: millones con un decimal si existe (`1M`, `1.1M`, `1.6M`).
- `10000000` o mas: millones enteros truncados (`10M`, `11M`).

Persistencia:
- Las semillas incluidas en build viven en `Assets/StreamingAssets/db/`.
- Los datos runtime se guardan en `Application.persistentDataPath/db/`.
- `player-records.json.totalShrimps` debe sobrevivir al cierre del juego.
- `player-records.json.totalShrimpsCollected` aumenta al recolectar camarones, no al recibir reembolsos.
- `player-records.json.bestScore` se actualiza al terminar una run.
- `player-profile.json` guarda `permanentUpgrades`, `skins` y `runGadgetUnlocks`.
- La skin default debe existir como `skin.default` en `player-profile.json.skins.unlockedSkinIds` y `equippedSkinId`.
- `unlockables-catalog.json` debe contener `skin.default`, `gadget.shell_shield`, `gadget.ink_bottle`, `upgrade.ink_pulse_duration`, `upgrade.ink_pulse_recharge_rate`, `upgrade.shrimp_multiplier` y `upgrade.score_multiplier`.
- `local-leaderboard.json` debe ordenar entradas por `score` descendente y limitarse a `maxEntries`.
- Settings no deben aparecer en estos JSON.

Pruebas de tienda out-of-game:
- En `MainMenu`, escribir `SONICYNOTA7` de forma sucesiva, sin campo de input, debe acreditar `676700` camarones de muestra por cada ingreso completo. El atajo no tiene limite de usos: 10 ingresos suman `6767000` camarones. Usa credito directo de saldo: no aplica multiplicador de camarones y no aumenta `totalShrimpsCollected`.
- `ShopMenu` contiene exactamente un `OutOfGameShopManager`, un `ShrimpCounter` prefab y un `OptionsMenu` prefab de escala independiente.
- `ShopMenu/Panel/ProductInfoBlock` contiene `NombreProducto`, `DescripcionProducto` y `PrecioProducto`, y los tres campos equivalentes de `OutOfGameShopManager` los referencian desde Inspector.
- Al seleccionar una mejora, el bloque actualiza nombre, nivel/descripcion y precio sin crear UI por codigo ni alterar el arte de las vitrinas.
- Los cuatro hitboxes superiores seleccionan, en orden, duration, recharge rate, shrimp multiplier y score multiplier; no deben modificar sprites ni layout de las vitrinas.
- `ComprarBoton` no compra sin una seleccion valida; con una mejora seleccionada debe delegar a `PermanentShopService` y refrescar el estado del boton.
- `VolverBoton` vuelve a `MainMenu` mediante su listener persistente.
- Con el catalogo actual, las skins visibles se filtran por `shopSpriteResourcePath`; las flechas de pagina deben habilitarse solo si hay mas skins visibles que slots.
- Seleccionar una skin no poseida debe mostrar precio compacto y permitir compra si hay saldo.
- Seleccionar una skin poseida debe permitir equiparla si no esta equipada; una skin equipada debe mostrar estado de equipada y no volver a gastar camarones.
- Los estados visuales de skins son `Buyed` para comprada no equipada y `Selected` para equipada.
- Los precios de ShopMenu deben usar el mismo formato compacto que `ShrimpCounterDisplay`.
- `PermanentShopService.TryPurchaseSkin()` debe descontar camarones solo si la skin existe, esta desbloqueada por meta y no estaba comprada.
- `PermanentShopService.TryPurchasePermanentUpgradeLevel()` debe respetar `maxLevel`, `basePrice` y `priceGrowthMultiplier`.
- El resultado debe expresarse mediante `PermanentShopPurchaseResult`; la UI no debe inferir por su cuenta falta de saldo, item desconocido o nivel maximo.
- Las compras permanentes no deben tocar `RuntimeGadgetInventory`.

Pruebas de efectos permanentes:
- Subir `upgrade.ink_pulse_duration` aumenta `InkPulseController.PulseDuration`.
- Subir `upgrade.ink_pulse_recharge_rate` aumenta `InkPulseController.ChargeRate`.
- Subir `upgrade.shrimp_multiplier` aumenta la cantidad agregada por `ShrimpRuntimeWallet.Add`.
- Subir `upgrade.score_multiplier` aumenta el score producido por `RunProgressionDirector`.

## Lore comics

Scripts: `LoreComicPresenter`

Prefab: `Assets/Content/Prefabs/UI/Menus/LoreComic.prefab`

Instalaciones esperadas:
- `MainMenu` para inicio de partida.
- `GameRoot_ZonaEpipelagica` para portal y derrota de zona epi.
- `GameRoot_ZonaAbisopelagica` para portal y derrota de zona abi.
- `GameRoot_ZonaTutorial` para compatibilidad de flujo tutorial.

Parametros ajustables:

| Campo | Script | Que controla |
| --- | --- | --- |
| `entries` | `LoreComicPresenter` | Catalogo local de eventos, zona, sprites, duracion y boton. |
| `displaySeconds` | `LoreComicEntry` | Duracion minima en tiempo real. |
| `waitForContinue` | `LoreComicEntry` | Si el flujo espera confirmacion tras la duracion. |
| `showContinueButton` | `LoreComicEntry` | Si se muestra el boton durante la espera. |
| `pauseTimeWhileShowing` | `LoreComicPresenter` | Si el comic congela `Time.timeScale` mientras esta visible. |

Pruebas:
- En `MainMenu`, presionar Play debe mostrar el comic de inicio antes de cargar gameplay.
- En portal `ZonaEpipelagica -> ZonaAbisopelagica`, debe mostrarse la vineta de direccion antes de la carga.
- En portal `ZonaAbisopelagica -> ZonaEpipelagica`, debe mostrarse la vineta inversa antes de la carga.
- Al entrar en Game Over, debe mostrarse una vineta de derrota de la zona actual antes del menu de derrota.
- Al tocar el primer `DealerFish` de la run, debe mostrarse `ShopInGameFirst` antes de abrir la tienda.
- Al salir de esa primera tienda tras comprar, debe mostrarse `ShopInGameLastPurchased`.
- Al salir de esa primera tienda sin comprar, debe mostrarse `ShopInGameLastNoPurchase`.
- Las siguientes tiendas de la misma run no deben repetir comics de primera entrada/salida.
- Si una escena no tiene `LoreComicPresenter`, el flujo no debe bloquearse.
- Si un evento no tiene entrada valida, no debe mostrarse un panel vacio.
- `ContinuarBoton` debe tener un listener persistente hacia `LoreComicPresenter.Continue()` dentro del prefab.

Regla visual:
- Los sprites de `Assets/Content/Art/ComicLore/` deben permanecer en sus carpetas de dominio (`Inicio`, `Portales`, `Derrota/*`, `Tienda`) y con `.meta` estable. No se evalua arte final en QA tecnica, solo referencias y flujo.

## Tutorial

Scripts: `TutorialDirector`, `TutorialPresentationController`, `TutorialStep`

Nodo esperado: `ZonaTutorial/GameRoot/Systems/GameSession`

Parametros ajustables:

| Campo | Script | Que controla |
| --- | --- | --- |
| `initialStep` | `TutorialDirector` | Paso inicial de la secuencia pedagogica. |
| `suppressScoreDuringTutorial` | `TutorialDirector` | Mantiene `RuntimeRunScore` en cero durante tutorial. |
| `movementRequiredVerticalDelta` | `TutorialDirector` | Desplazamiento vertical requerido para validar movimiento. |
| `grazeRequiredChargeRatio` | `TutorialDirector` | Porcentaje de Ink-Pulse requerido para validar graze. |
| `requiredShrimpCount` | `TutorialDirector` | Camarones que deben recolectarse desde el baseline del paso. |
| `firstShopInkBottlePrice` | `TutorialDirector` | Precio forzado de Ink Bottle en la primera tienda tutorial. |
| `secondShopShellShieldPrice` | `TutorialDirector` | Precio forzado de Shell Shield en la segunda tienda tutorial. |
| `forcedShopOpenFallbackSeconds` | `TutorialDirector` | Tiempo tras el cual la tienda tutorial se abre si el jugador no toca DealerFish. |
| `inkPulseAssistDelaySeconds` | `TutorialDirector` | Retardo antes de dejar Ink-Pulse listo durante la asistencia de SS Carnage. |
| `usePresentationPhase` | `TutorialDirector` | Si cada paso entra primero a fase de presentacion. |
| `defaultPresentationSeconds` | `TutorialDirector` | Duracion base del comic/presentacion. Valor actual: `7`. |
| `defaultPracticeSeconds` | `TutorialDirector` | Ventana base para probar la mecanica. Valor actual: `10`. |
| `autoAdvanceWhenPracticeExpires` | `TutorialDirector` | Permite autoavance al expirar practica en pasos no bloqueantes. |
| `stepTimingOverrides` | `TutorialDirector` | Overrides por paso para tiempos y autoavance. |
| `controlLevelSpawner` | `TutorialDirector` | Si el director habilita/deshabilita el spawner durante tutorial. |
| `levelSpawnerEnabledFromStep` | `TutorialDirector` | Paso desde el cual se habilita `LevelSpawner` si la compuerta esta activa. |
| `controlBossDirector` | `TutorialDirector` | Si el director habilita/deshabilita el evento de boss durante tutorial. |
| `bossDirectorEnabledFromStep` | `TutorialDirector` | Paso desde el cual se habilita `BossEventDirector` si la compuerta esta activa. |
| `freezeGameplay` | `TutorialPresentationController` | Si `Presentation` congela el juego con `Time.timeScale = 0`. |
| `suppressInkPulse` | `TutorialPresentationController` | Si Ink-Pulse queda bloqueado durante presentacion. |
| `darkenDuringPresentation` | `TutorialPresentationController` | Si se muestra el overlay oscuro durante presentacion. |
| `presentationAlpha` | `TutorialPresentationController` | Opacidad objetivo del dimmer; valor actual `0.35`. |
| `fadeSeconds` | `TutorialPresentationController` | Duracion del fade visual usando tiempo real. |

Regla de prueba: el tutorial debe avanzar por `Movement -> GrazeCharge -> InkPulseObstacle -> CollectShrimps10 -> FirstShopOpen -> BuyInkBottle -> InkBottleBarrier -> CarnageIntro -> CarnageInkPulseAssist -> CarnageInkPulseResolve -> SecondShopOpen -> BuyShellShield -> ProtectedHitSetup -> ProtectedHitResolved -> PortalSpawn -> PortalEnter -> VisualZoneShift -> FinalEnemy -> FinalDeath -> Completed` sin modificar `RunProgressionDirector` ni `LevelSpawner` para casos pedagogicos puntuales.

Regla de puntaje: `GameRoot_ZonaTutorial.prefab` debe mantener `RunProgressionDirector.scorePerSecond = 0`; al terminar el tutorial, el score mostrado debe seguir en cero.

Regla de presentacion: durante cada `TutorialPhase.Presentation`, la escena debe congelarse, Ink-Pulse no debe activarse y `TutorialPresentationOverlay/Dimmer` debe oscurecer la pantalla sin bloquear raycasts.

## Valores que no conviene tocar como balance

- Campos `References`: conectan dependencias; no cambian dificultad.
- Boundaries: se definen por jerarquia fisica, no por valores manuales ni referencias serializadas.
- Tags manuales fuera de catalogos: deben venir de `EnemyTagCatalog` o `GameplayTagCatalog`.
- Layers de prefabs: deben seguir la auditoria de jerarquia para que colisiones y limpieza funcionen.
- `firstSlotKey` y `secondSlotKey` salvo verificacion de bug: la convencion activa es `Q` primero y `W` segundo.
