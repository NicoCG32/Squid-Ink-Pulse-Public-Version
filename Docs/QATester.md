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

## Progresion de dificultad

Script: `RunProgressionDirector`

Nodo esperado: `GameSession`

Parametros ajustables:

| Campo | Que controla | Efecto esperado al subirlo |
| --- | --- | --- |
| `secondsToMaxIntensity` | Tiempo necesario para llegar a intensidad maxima dentro del ciclo. | La dificultad escala mas lento. |
| `postBossIntensityFloor` | Piso minimo de intensidad tras superar un boss. | La run no baja tanto despues del boss. |
| `minScrollSpeed` | Velocidad horizontal minima. | La partida empieza mas rapida. |
| `maxScrollSpeed` | Velocidad horizontal maxima. | El late game avanza mas rapido. |
| `maxSpawnInterval` | Intervalo de spawn cuando la intensidad es baja. | Aparecen menos objetos al inicio. |
| `minSpawnInterval` | Intervalo de spawn cuando la intensidad es alta. | El late game respira mas si se sube; se satura mas si se baja. |
| `bossActiveSpawnIntervalMultiplier` | Multiplicador del intervalo durante boss activo. | Si sube, aparecen menos obstaculos durante boss; si baja, aparecen mas. |
| `postBossSpawnIntervalMultiplier` | Multiplicador del intervalo en reposo post-boss. | Si sube, el reposo tiene menos presion. |
| `maxBossInterval` | Tiempo maximo hasta boss en baja intensidad. | El primer boss puede tardar mas. |
| `minBossInterval` | Tiempo minimo hasta boss en alta intensidad. | Los bosses aparecen menos seguido si se sube. |
| `postBossWindowSeconds` | Duracion del reposo tras resolver boss. | El jugador recibe una pausa mayor. |

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

## Tienda temporal

Scripts: `LevelSpawner`, `DealerFish`, `InGameShopManager`

Nodos esperados: `LevelSpawner`, prefab `DealerFish`, `UI/InGameShopManager`

Parametros ajustables:

| Campo | Script | Que controla |
| --- | --- | --- |
| `enableDealerFishSpawns` | `LevelSpawner` | Activa o desactiva aparicion de tienda. |
| `firstDealerFishSpawnDelay` | `LevelSpawner` | Tiempo hasta el primer DealerFish. |
| `dealerFishSpawnInterval` | `LevelSpawner` | Intervalo entre DealerFish posteriores. |
| `offerDurationSeconds` | `InGameShopManager` | Tiempo disponible para comprar. |
| `pauseGameplayWhileOpen` | `InGameShopManager` | Si la tienda congela gameplay mientras corre en tiempo real. |
| `globalPriceMultiplier` | `InGameShopManager` | Multiplicador general de precios. |
| `intensityPriceMultiplier` | `InGameShopManager` | Cuanto sube el precio por intensidad actual. |
| `cyclePriceMultiplier` | `InGameShopManager` | Cuanto sube el precio por ciclos/bosses superados. |
| `offers[].basePriceOverride` | `InGameShopManager` | Precio base alternativo para una oferta concreta. |
| `textPulseAmplitude` | `InGameShopManager` | Magnitud de pulso para `B` y `Precio`. |
| `textPulseFrequency` | `InGameShopManager` | Velocidad del pulso visual de tienda. |

Reglas vigentes:

- Comprar usa tecla `B`.
- `SinSaldo` aparece solo despues de intentar comprar con `B` sin camarones suficientes.
- La tienda puede ofrecer un gadget repetido, pero no permite comprarlo si ya existe en inventario.

## Portales

Script de contacto: `ScenePortal`

Prefab: `Assets/Content/Prefabs/Portals/ScenePortal.prefab`

Script de aparicion: `LevelSpawner`

Script de rutas: `SceneFlowController`

Parametros ajustables:

| Campo | Que controla | Nota de test |
| --- | --- | --- |
| `portalPrefab` | Prefab de portal que se instancia en runtime. | Debe apuntar a `ScenePortal.prefab`. |
| `portalSpawnPolicy` | Regla de aparicion del portal. | `ZonaEpipelagica` usa `PostBossWindow`; `ZonaExe` usa `AlwaysInterval`. |
| `portalSpawnedParent` | Contenedor donde se agrupan los portales instanciados. | Debe apuntar al nodo `Portals`. |
| `firstPortalSpawnDelay` | Espera antes del primer portal o de la tirada post-boss. | `ZonaEpipelagica` usa `3s`; `ZonaExe` usa `20s`. |
| `postBossPortalSpawnChance` | Probabilidad de que aparezca portal tras el delay post-boss. | Solo aplica a `PostBossWindow`; `1` significa garantizado. |
| `portalSpawnInterval` | Intervalo entre portales posteriores. | `ZonaExe` usa `20s`. |
| `requireNoActivePortal` | Evita crear otro portal si uno anterior sigue vivo. | Debe estar activo por defecto. |
| `primaryGameplaySceneName` | Zona base o retorno. | Vive en `SceneFlowController`; por defecto `ZonaEpipelagica`. |
| `secondaryGameplaySceneName` | Zona alterna. | Vive en `SceneFlowController`; por defecto `ZonaExe`. |

Reglas vigentes:

- El portal usa tag `Portal`, no `Shrimp` ni `Collectible`.
- El portal usa capa `Collectible` para participar en colisiones de mundo.
- `ZonaExe` debe estar habilitada en Build Settings.
- Cruzar un portal conserva gadgets e Ink-Pulse.
- Entrar en Game Over reinicia gadgets e Ink-Pulse.

## Iluminacion de ZonaExe

Script: `ZoneLightingController`

Nodo esperado: `Enviroment/ZoneLightingController` en `ZonaExe`

Parametros ajustables:

| Campo | Que controla | Efecto esperado al subirlo |
| --- | --- | --- |
| `darkAlpha` | Opacidad base del overlay oscuro. | `ZonaExe` se ve mas oscura. |
| `litAlpha` | Opacidad minima durante light graze. | Si sube, el revelado conserva mas oscuridad. |
| `litHoldSeconds` | Tiempo que permanece revelado tras pasar cerca de una fuente. | La zona queda clara por mas tiempo. |
| `fadeToLitSpeed` | Velocidad para aclarar el fondo. | El feedback de luz se siente mas inmediato. |
| `fadeToDarkSpeed` | Velocidad para volver a oscuridad. | La oscuridad vuelve mas rapido. |
| `overlayPadding` | Margen extra de cobertura respecto a la camara. | Evita bordes sin overlay en aspect ratios amplios. |
| `lightGrazeRadius` | Distancia de activacion entre BabySquid y fuentes. | La luz se activa desde mas lejos. |

Reglas vigentes:

- `LightGraze` es visual: no carga Ink-Pulse y no reemplaza `GrazeDetector`.
- Las entidades relevantes tienen `LightGrazeSource`; no se balancean desde cada prefab.
- El BabySquid usa `LightGrazeProbe` para detectar fuentes cercanas.
- En `ZonaEpipelagica` y `ZonaTutorial`, la sonda no hace nada mientras no exista `ZoneLightingController`.

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

- La carga del Ink-Pulse persiste al cruzar portales.
- Si el Ink-Pulse esta en `Active` al cruzar, persiste con su tiempo restante.
- La carga vuelve a cero al entrar en Game Over.
- No puede activarse mientras `InGameShopManager` esta en `ShopEventState.Offering`.

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
| `fallSpeed` | Velocidad de caida cuando no esta expandido. | Baja mas rapido. |
| `expandedRiseSpeedMultiplier` | Multiplicador de subida al expandirse. | Sube mas rapido durante amenaza. |
| `proximityRadius` | Distancia para expandirse. | Se activa desde mas lejos. |
| `expandedScaleMultiplier` | Escala objetivo al expandirse. | Ocupa mas espacio. |
| `expansionSmoothSpeed` | Velocidad de interpolacion de escala. | La expansion se ve mas inmediata. |

Estos campos no se ajustan en el prefab `PezGlobo`.

El prefab `PezGlobo` debe tener un unico `CircleCollider2D` en la raiz. La expansion escala el `Transform`, por lo que el collider circular acompana el crecimiento visual y fisico.

### Mina

La mina no tiene script propio todavia. Su balance actual depende de:

- Perfil `EnemyMina` en `LevelSpawner.enemyProfiles`.
- `lowerZoneSpawnCoverage`.
- Collider y escala del prefab.

### Cana de pescar

La cana no tiene script propio todavia. Su balance actual depende de:

- Perfil `EnemyCanaPescar` en `LevelSpawner.enemyProfiles`.
- `fishingRodEnemyInterval`.
- Collider y escala del prefab.

Reglas vigentes:
- La caña regular aparece a la misma altura Y del jugador.
- La caña regular se fuerza solo fuera de `BossActive`.
- Un futuro anzuelo del SS Carnage debe probarse como prefab/ataque de boss independiente, no como excepcion del spawner regular.

## SS Carnage

Scripts: `BossEventDirector`, `SSCarnageController`, `SSCarnageNetWall`

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
