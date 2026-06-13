# QATester

## Proposito

Este documento enumera los parametros que actualmente pueden ajustarse para probar, medir y balancear la experiencia. La regla de trabajo es distinguir entre:

- Parametros de balance: valores que el tester puede variar para evaluar dificultad, ritmo, recompensa o lectura visual.
- Referencias tecnicas: campos que conectan objetos de escena, prefabs, camaras, colliders o textos. No deberian modificarse durante balance salvo que se este corrigiendo cableado.

Los cambios deben probarse de uno en uno cuando sea posible. Si se modifican varios parametros al mismo tiempo, anotar el conjunto exacto porque el efecto observado ya no puede atribuirse a una sola causa.

## Metodo recomendado

1. Definir una hipotesis breve: por ejemplo, "la tienda aparece demasiado tarde" o "el Pez Globo presiona demasiado".
2. Cambiar un solo parametro.
3. Ejecutar una run corta y observar el efecto.
4. Registrar valor anterior, valor nuevo y resultado.
5. Repetir hasta encontrar un rango aceptable.

## Contrato no balanceable: boundaries

Los limites verticales no son parametros de QA. Son infraestructura de escena.

Toda zona jugable debe contener exactamente estos nodos:

| Dominio | Contenedor | Hijos obligatorios |
| --- | --- | --- |
| Jugador | `PlayerBoundaries` | `TopBoundary`, `BottomBoundary` |
| Camara | `CameraBoundaries` | `TopBoundary`, `BottomBoundary` |

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

## Score

Scripts: `RunProgressionDirector`, `RuntimeRunScore`, `ScoreCounterDisplay`

Nodos esperados: `GameSession` y objeto UI `Score` si existe en la escena.

Reglas vigentes:
- El score crece durante `GameSessionState.Playing`.
- Persiste al cruzar portales.
- Debe conservar el valor visible al pasar desde `ZonaEpipelagica` a `ZonaAbisopelagica`.
- Deja de crecer durante `RunEventState.Transitioning`, es decir, desde el contacto con portal hasta la carga de escena.
- Se reinicia al entrar en `GameSessionState.GameOver`.
- Al pulsar `Reintentar`, la escena cargada debe ser `ZonaEpipelagica`, incluso si la derrota ocurrio en `ZonaAbisopelagica`.
- `ScoreCounterDisplay` solo presenta el numero; no calcula progresion.
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

Script: `LevelSpawner`

Nodo esperado: `LevelSpawner`

Parametros ajustables:

| Campo | Que controla | Efecto esperado al subirlo |
| --- | --- | --- |
| `timeBetweenSpawns` | Intervalo base cuando no hay `RunProgressionDirector`. | Menos spawns si no hay progresion conectada. |
| `spawnDistanceFromCameraRight` | Distancia horizontal desde el borde derecho de camara. | Los objetos aparecen mas lejos y tardan mas en llegar. |
| `verticalPadding` | Margen vertical contra camara y boundaries. | Los spawns se alejan mas de los limites. |
| `coinSpawnChance` | Probabilidad de que el spawn sea camaron. | Hay mas recompensa y menos peligro. |
| `rareCoinSpawnChanceWithinCoins` | Probabilidad de camaron x10 cuando ya salio camaron. | Aumenta la economia de la run. |
| `fishingRodEnemyInterval` | Cada cuantos enemigos de juego normal se fuerza una cana. | La cana aparece menos seguido si se sube. |
| `upperZoneSpawnCoverage` | Porcion superior disponible para Pez Globo. | El Pez Globo tiene mas dispersion vertical. |
| `lowerZoneSpawnCoverage` | Porcion inferior disponible para Mina. | La Mina tiene mas dispersion vertical. |

`enemyProfiles` define el peso por enemigo:

| Campo | Que controla | Nota de test |
| --- | --- | --- |
| `prefab` | Prefab instanciado. | Debe tener tag esperado y collider correcto. |
| `enemyTag` | Identidad logica del enemigo. | Usar `EnemyMina`, `EnemyPezGlobo` o `EnemyCanaPescar`. |
| `baseWeight` | Peso relativo de aparicion. | Mas alto implica mayor frecuencia relativa. |
| `minIntensity` | Intensidad minima para poder aparecer. | Sirve para retrasar enemigos complejos. |
| `spawnIntervalMultiplier` | Modificador del intervalo despues de ese enemigo. | Mas alto deja mas aire tras ese spawn. |

Valor vigente de referencia: `coinSpawnChance = 0.225`, equivalente a tres cuartos del valor anterior `0.3`.

## Tienda temporal

Scripts: `LevelSpawner`, `DealerFish`, `InGameShopManager`

Nodos esperados: `LevelSpawner`, prefab `DealerFish`, `UI/InGameShopManager`

Parametros ajustables:

| Campo | Script | Que controla |
| --- | --- | --- |
| `enableDealerFishSpawns` | `LevelSpawner` | Activa o desactiva aparicion de tienda. |
| `firstDealerFishSpawnDelay` | `LevelSpawner` | Tiempo base hasta el primer DealerFish. |
| `dealerFishSpawnInterval` | `LevelSpawner` | Intervalo base entre DealerFish posteriores. |
| `dealerFishIntervalRandomMultiplierMin` | `LevelSpawner` | Multiplicador aleatorio minimo del intervalo base. |
| `dealerFishIntervalRandomMultiplierMax` | `LevelSpawner` | Multiplicador aleatorio maximo del intervalo base. |
| `dealerFishSpawnDistanceFromCameraRight` | `LevelSpawner` | Distancia horizontal propia del DealerFish desde el borde derecho de camara. |
| `dealerFishSpawnZoneMin` | `LevelSpawner` | Inicio normalizado de la zona vertical de aparicion, dentro de la mitad inferior. |
| `dealerFishSpawnZoneMax` | `LevelSpawner` | Fin normalizado de la zona vertical de aparicion, limitado a la mitad inferior. |
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
- Formula de precio vigente: `ceil(((score / scorePriceStep) + 1) * randomPriceMultiplier * precioBaseMinimo * globalPriceMultiplier)`.
- Al colisionar con `DealerFish`, el objeto intenta abrir tienda una vez y permanece visible; su collider queda desactivado para no reabrir tienda mientras el jugador lo atraviesa.
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
| `portalPrefab` | Prefab de portal que se instancia en runtime. | Debe apuntar a `ScenePortal.prefab`. |
| `portalSpawnPolicy` | Regla de aparicion del portal. | `ZonaEpipelagica` usa `PostBossWindow`; `ZonaAbisopelagica` usa `AlwaysInterval`. |
| `portalSpawnedParent` | Contenedor donde se agrupan los portales instanciados. | Debe apuntar al nodo `Portals`. |
| `firstPortalSpawnDelay` | Espera antes del primer portal o de la tirada post-boss. | `ZonaEpipelagica` usa `3s`; `ZonaAbisopelagica` usa `20s`. |
| `postBossPortalSpawnChance` | Probabilidad de que aparezca portal tras el delay post-boss. | Solo aplica a `PostBossWindow`; `1` significa garantizado. |
| `portalSpawnInterval` | Intervalo entre portales posteriores. | `ZonaAbisopelagica` usa `20s`. |
| `requireNoActivePortal` | Evita crear otro portal si uno anterior sigue vivo. | Debe estar activo por defecto. |
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

Nodo esperado: `Enviroment/ZoneLightingController` en `ZonaAbisopelagica`

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

Reglas vigentes:

- `LightGraze` es visual: no carga Ink-Pulse y no reemplaza `GrazeDetector`.
- En modo compuesto, `LayerBlack` usa una textura generada y `maskInteraction = None`.
- `VisibleOutsideMask` solo corresponde al modo fallback con `SpriteMask`.
- La instancia `Squid` de `BabySquid.prefab` tiene `LightGrazeSource` en `ZonaAbisopelagica`.
- `LevelSpawner` agrega `LightGrazeSource` a entidades runtime solo si existe `ZoneLightingController`.
- `SSCarnage` y `BossNetWall` no participan porque no aparecen en `ZonaAbisopelagica`.

## Gadgets e inventario

Scripts: `GadgetDefinitions`, `GadgetShopItem`, `PlayerGadgetInventory`, `GadgetInventoryHud`

Parametros ajustables:

| Campo | Script | Que controla |
| --- | --- | --- |
| `GadgetCatalog.GetBaseShopPrice()` | `GadgetDefinitions` | Precio base por tipo de gadget. Actualmente es codigo, no Inspector. |
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
- Los gadgets y slots persisten al cruzar portales.
- Los gadgets y slots se reinician al entrar en Game Over.

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

Owner de parametros: `LevelSpawner.pufferfishTuning`

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

La mina no tiene script propio todavia. Su balance actual depende de:

- Perfil `EnemyMina` en `LevelSpawner.enemyProfiles`.
- `lowerZoneSpawnCoverage`.
- Collider y escala del prefab.

### Cana de pescar

Script de comportamiento: `FishingRodEnemy`

Owner de parametros: `LevelSpawner.fishingRodTuning`

Parametros ajustables:

| Campo | Que controla | Efecto esperado al subirlo |
| --- | --- | --- |
| `dropSpeed` | Velocidad vertical de bajada hacia la Y capturada del jugador. | La caña cae mas brusca y rapidamente. |
| `startYOffsetBelowTopBoundary` | Distancia bajo el `TopBoundary` desde donde empieza la bajada. | La caña nace mas abajo si se sube. |
| `arriveDistance` | Tolerancia para considerar que llego a la Y objetivo. | Detiene el movimiento con menos precision si se sube. |
| `horizontalLeadTimePaddingSeconds` | Margen temporal agregado al calculo de distancia horizontal del anzuelo. | El anzuelo aparece mas lejos cuando el jugador va rapido. |
| `minimumHorizontalLeadDistance` | Distancia minima propia de la cana desde el borde derecho de camara. | Evita que aparezca demasiado cerca a velocidades bajas. |

Tambien depende de:
- Perfil `EnemyCanaPescar` en `LevelSpawner.enemyProfiles`.
- `fishingRodEnemyInterval`.
- Collider y escala del prefab.

Reglas vigentes:
- La caña regular captura la altura Y del jugador al spawnear.
- Luego baja verticalmente desde el top del rango jugable hasta esa Y.
- La distancia X de aparicion se calcula con la velocidad horizontal actual del jugador y el tiempo estimado de caida.
- No persigue al jugador despues de capturar la Y.
- La caña regular se fuerza solo fuera de `BossActive`.
- Un futuro anzuelo del SS Carnage debe probarse como prefab/ataque de boss independiente, no como excepcion del spawner regular.

## SS Carnage

Scripts: `BossEventDirector`, `SSCarnageController`, `SSCarnageNetWall`

Escena esperada actual: `ZonaEpipelagica`. `ZonaAbisopelagica` no debe tener `BossEventDirector`, `SSCarnageManager` ni `BossNetWall`; si aparece un warning de referencias faltantes en esa zona, el objeto es legacy y debe limpiarse, no completarse con referencias ficticias.

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
| `netSpawnDistanceFromCameraRight` | `SSCarnageController` | Distancia de aparicion de la red desde camara. |
| `netViewportY` | `SSCarnageController` | Altura relativa de spawn de red. |
| `deployNetOnStart` | `SSCarnageController` | Si el ataque inicia automaticamente. |

`SSCarnageNetWall` ajusta altura visual y volumen de colision automaticamente desde `PlayerBoundaries`. Esa altura no se balancea desde Inspector, y la red rota queda como feedback visual fijo.

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

`HorizontalTracker` no tiene parametros de balance: solo sigue la camara asignada. `DestroyOffscreen` no tiene parametros de balance: sigue el borde izquierdo de la camara y destruye enemigos, camarones, collectibles y portales que ya salieron de pantalla. La referencia `targetCamera` es cableado tecnico, no un valor de balance.

## UI y menus

Scripts: `PauseMenuManager`, `GameOverMenuManager`, `MenuButtonAnimation`, `MenuBubbles`

Parametros ajustables:

| Campo | Script | Que controla |
| --- | --- | --- |
| `fadeDuration` | `PauseMenuManager` / `GameOverMenuManager` | Duracion del fundido. |
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

## Economia de camarones

Scripts: `ShrimpValue`, `ShrimpRuntimeWallet`, `ShrimpCounterDisplay`

Parametros ajustables:

| Campo | Script | Que controla |
| --- | --- | --- |
| `amount` | `ShrimpValue` | Valor de cada camaron recogible. |
| `prefix` | `ShrimpCounterDisplay` | Texto previo al numero en HUD. |

`ShrimpRuntimeWallet` no tiene parametros de balance en Inspector. Es almacenamiento runtime y API de suma/gasto.

## Valores que no conviene tocar como balance

- Campos `References`: conectan dependencias; no cambian dificultad.
- Boundaries: se definen por jerarquia fisica, no por valores manuales ni referencias serializadas.
- Tags manuales fuera de catalogos: deben venir de `EnemyTagCatalog` o `GameplayTagCatalog`.
- Layers de prefabs: deben seguir la auditoria de jerarquia para que colisiones y limpieza funcionen.
- `firstSlotKey` y `secondSlotKey` salvo verificacion de bug: la convencion activa es `Q` primero y `W` segundo.
