# Sistemas de gameplay

## Alcance

Este documento agrupa los sistemas que definen la experiencia directa del jugador: movimiento, Ink-Pulse, graze, light graze visual, colisiones, camarones, inventario de gadgets y tienda temporal.

## PlayerMovement

Archivo: `Assets/Implementation/Code/Player/Movement/PlayerMovement.cs`

Prefab canonico: `Assets/Content/Prefabs/Player/BabySquid.prefab`

Responsabilidad:
- Mover al jugador en horizontal con avance continuo.
- Ajustar la posicion vertical segun input del mouse.
- Aplicar limites verticales usando `PlayerBoundaries`.
- Cambiar velocidad y tilt durante Ink-Pulse.
- Opcionalmente elegir una Y inicial aleatoria dentro de `PlayerBoundaries`.

Contrato de limites:
- No usa `minY` ni `maxY` serializados.
- No recibe `topBorder` ni `bottomBorder` por Inspector.
- Si `PlayerBoundaries/TopBoundary` o `PlayerBoundaries/BottomBoundary` faltan, el problema es de escena.

Contrato de prefab:
- Las escenas jugables usan una instancia llamada `Squid`, pero la fuente editable es `BabySquid.prefab`.
- El prefab base no guarda referencias externas a sesion, camara, HUD, progression director ni boundaries.
- Las instancias de escena tienen esas referencias externas asignadas en Inspector.
- Los componentes del jugador resuelven esas referencias en runtime solo como respaldo.
- Los cambios de collider, visuales base, `GrazeZone`, inventario o Ink-Pulse deben aplicarse al prefab.

## InkPulseController

Archivo: `Assets/Implementation/Code/Player/Abilities/InkPulseController.cs`

Estado runtime: `Assets/Implementation/Code/Player/Abilities/RuntimeInkPulseState.cs`

Feedback musical: `Assets/Implementation/Code/Audio/InkPulseMusicCrossfader.cs`

Feedback visual: `Assets/Implementation/Code/Player/Visual/PlayerVisualStateController.cs`

Responsabilidad:
- Administrar la carga del recurso Ink-Pulse.
- Exponer `InkPulseState`.
- Activar y finalizar el pulso.
- Notificar cambios de carga y estado a UI o sistemas externos.
- Permitir que `Ink-Bottle` fuerce el estado `Ready` mediante `TryForceReady()`.
- Persistir carga, estado activo y tiempo restante entre portales.
- Reiniciarse cuando `GameSessionController` entra en `GameSessionState.GameOver`.
- Bloquear activacion manual mientras `InGameShopManager` esta mostrando una oferta temporal.
- Bloquear activacion nueva mientras `PlayerStateController` esta en `PlayerRuntimeState.PortalTransition`.
- Exponer eventos para feedback externo, incluida la mezcla musical del soundtrack normal y `INK`.
- Exponer duracion y tiempo restante para que animaciones puedan ajustarse al estado `Active`.

Input:
- Click izquierdo o tecla `Space` intentan activar el pulso.
- Ambos inputs pasan por la misma validacion: no activan durante tienda, portal, muerte, Game Over ni antes de `Ready`.

Estados:
- `Idle`
- `Charging`
- `Ready`
- `Active`

Regla de audio:
- `InkPulseMusicCrossfader` vive en el nodo `Soundtrack` de la escena.
- Las dos pistas se reproducen sincronizadas desde el mismo tiempo DSP.
- Al entrar en `Active`, la mezcla cruza hacia la pista `INK`.
- Al salir de `Active`, la mezcla vuelve a la pista normal.
- Para dos mezclas completas del mismo tema, el crossfade lineal complementario es el valor por defecto porque evita sumar ambas mezclas a volumen completo.

## GrazeDetector

Archivo: `Assets/Implementation/Code/Player/Interaction/GrazeDetector.cs`

Responsabilidad:
- Detectar proximidad util con enemigos u obstaculos.
- Alimentar la carga del Ink-Pulse sin requerir colision.
- Reconocer amenazas mediante `EnemyTagCatalog`, no mediante tags editables en Inspector.

## Light graze visual

Archivos:
- `Assets/Implementation/Code/World/Lighting/ZoneLightingController.cs`
- `Assets/Implementation/Code/World/Lighting/LightGrazeSource.cs`

Responsabilidad:
- Oscurecer `ZonaAbisopelagica` mediante un overlay de escena.
- Componer una unica textura de oscuridad que revela el entorno alrededor de entidades con `LightGrazeSource`.
- Suavizar el borde de cada zona revelada sin acumular opacidad cuando dos luces se cruzan.
- Mantenerse independiente del `GrazeDetector` y del `GrazeZone`.
- No cargar Ink-Pulse ni modificar economia, dano o colisiones.

Reglas:
- Los parametros visuales viven en `ZoneLightingController`.
- En modo compuesto, `LayerBlack` usa una textura generada por `ZoneLightingController` y `SpriteRenderer.maskInteraction = None`.
- `SpriteRenderer.maskInteraction = VisibleOutsideMask` solo corresponde al fallback legacy con `SpriteMask`.
- La instancia `Squid` de `BabySquid.prefab` en `ZonaAbisopelagica` declara `LightGrazeSource` como override de escena.
- `LevelSpawner` agrega `LightGrazeSource` a camarones, enemigos, `DealerFish` y portales solo si la zona activa tiene `ZoneLightingController`.

## PlayerCollision

Archivo: `Assets/Implementation/Code/Player/Interaction/PlayerCollision.cs`

Responsabilidad:
- Resolver colisiones del jugador con amenazas u objetos relevantes.
- Delegar identificacion de enemigos en `EnemyTagCatalog`.
- Consultar `PlayerGadgetInventory` antes de declarar Game Over, para consumir `Shell Shield` si existe.
- Reconocer camarones mediante `GameplayTagCatalog.Shrimp`.
- Ignorar dano durante Ink-Pulse sin usar flags locales de destruccion de amenaza.

## Camarones

Archivos:
- `Assets/Implementation/Code/Player/Interaction/ShrimpCollector.cs`
- `Assets/Implementation/Code/Player/Interaction/ShrimpValue.cs`
- `Assets/Implementation/Code/Player/Interaction/ShrimpRuntimeWallet.cs`
- `Assets/Implementation/Code/Player/Profile/PersistentPlayerProfile.cs`
- `Assets/Implementation/Code/UI/HUD/ShrimpCounterDisplay.cs`

Responsabilidad:
- Registrar recoleccion de camarones.
- Definir el valor de cada recogible.
- Alimentar una billetera persistente respaldada por `player-profile.json`.
- Mostrar el total en HUD.

Estado actual:
- `ShrimpCoin` vale `1`.
- `ShrimpCoinX10` vale `10`.
- El total persiste fuera del juego mediante `PersistentPlayerProfile`.
- La tienda in-run gasta desde la misma billetera persistente.
- Los reembolsos usan `ShrimpRuntimeWallet.Refund` para no inflar `totalShrimpsCollected`.

## Score de run

Archivos:
- `Assets/Implementation/Code/Core/Session/RuntimeRunScore.cs`
- `Assets/Implementation/Code/Core/Session/RuntimePlayerPace.cs`
- `Assets/Implementation/Code/Core/Session/RunProgressionDirector.cs`
- `Assets/Implementation/Code/UI/HUD/ScoreCounterDisplay.cs`

Responsabilidad:
- Registrar avance abstracto de la partida como puntaje.
- Subir rapidamente mientras la sesion esta en gameplay activo.
- Persistir entre portales.
- Conservar el valor acumulado al pasar de `ZonaEpipelagica` a `ZonaAbisopelagica`.
- Pausar acumulacion desde el contacto con portal mientras la run esta en `RunEventState.Transitioning`.
- Reiniciarse al entrar en Game Over.
- Reiniciarse al pulsar reintentar, porque se inicia una run nueva desde `ZonaEpipelagica`.
- Alimentar sistemas de progresion como precios de tienda.
- Separar score y velocidad del flujo de intensidad de spawns.

Reglas:
- El score no es moneda y no se gasta.
- `RunProgressionDirector` acumula el valor; `ScoreCounterDisplay` solo lo muestra.
- El HUD puede tener un nodo `Score` con `TextMeshProUGUI`; la utilidad de escena le asigna `ScoreCounterDisplay`.
- `RuntimePlayerPace` acumula la progresion de velocidad del calamar y persiste entre portales.
- La velocidad horizontal normal crece de forma asintotica desde `minScrollSpeed` hacia `maxScrollSpeed`.
- La intensidad de spawn usa otra curva: baja a alta, boss, post-boss intenso; cruzar portal reinicia esa intensidad en la zona destino.

## Gadgets e inventario

Archivos:
- `Assets/Implementation/Code/Player/Inventory/GadgetId.cs`
- `Assets/Implementation/Code/Player/Inventory/GadgetActivationKind.cs`
- `Assets/Implementation/Code/Player/Inventory/GadgetCatalog.cs`
- `Assets/Implementation/Code/Player/Inventory/RuntimeGadgetInventory.cs`
- `Assets/Implementation/Code/Player/Inventory/PlayerGadgetInventory.cs`
- `Assets/Implementation/Code/Player/Inventory/GadgetShopItem.cs`
- `Assets/Implementation/Code/UI/HUD/GadgetInventoryHud.cs`

Responsabilidad:
- Inicializar inventario runtime.
- Registrar prefabs de gadget como mercancia comprable mediante `GadgetShopItem`.
- Almacenar gadgets en slots segun orden de adquisicion.
- Modelar posesion unica con `HasGadget`, no con contadores ni stacks.
- Mostrar tecla solo cuando el gadget del slot es activo.
- Activar `Gadget1` con `Q` y `Gadget2` con `W`.
- Persistir entre portales mediante `RuntimeGadgetInventory`.
- Reiniciarse cuando `GameSessionController` entra en `GameSessionState.GameOver`.

Gadgets implementados:
- `Shell Shield`: pasivo, se consume automaticamente para cancelar un Game Over.
- `Ink-Bottle`: activo, se consume si logra llevar Ink-Pulse a `Ready`.

Reglas de slot:
- El prefab no define si un gadget va en `Q` o `W`.
- El primer gadget activo en `Gadget1` usa `Q`.
- El segundo gadget activo en `Gadget2` usa `W`.
- Los pasivos ocupan slot visual, pero no muestran tecla.
- Ningun gadget es stackable.

## Tienda temporal de suministros

Archivos:
- `Assets/Implementation/Code/UI/Shop/InGameShopManager.cs`
- `Assets/Implementation/Code/World/Shop/DealerFish.cs`
- `Assets/Implementation/Code/Spawning/LevelSpawner.cs`

Responsabilidad:
- Instanciar `DealerFish` desde `LevelSpawner`.
- Ubicar `DealerFish` dentro de una zona normalizada configurable de la mitad inferior del rango entre `PlayerBoundaries`.
- Separar intervalo base de aparicion y variacion aleatoria de cadencia.
- Abrir un overlay temporal al colisionar con `DealerFish`.
- Seleccionar un gadget aleatorio desde ofertas configuradas.
- Mostrar icono, precio, tecla `B`, boton `Comprar`, contador y mensaje de saldo.
- Consumir camarones solo si la compra se concreta.
- Registrar el gadget comprado en `RuntimeGadgetInventory`.

Reglas:
- La tienda tiene duracion ajustable por `offerDurationSeconds`.
- Por defecto congela gameplay mientras el contador avanza en tiempo real.
- La compra se intenta con `B` o click sobre el boton `Comprar`.
- `SinSaldo` aparece solo despues de intentar comprar sin camarones suficientes.
- El precio se calcula desde score: `((score / 100000) + 1) * aleatorio(1, 2) * precioBaseMinimo`, con parametros equivalentes en `InGameShopManager`.
- Si el gadget ya existe en inventario, no se compra de nuevo.
- Si el contador llega a cero, la oferta se cierra.
- `DealerFish` permanece visible tras abrir tienda; su collider se desactiva para evitar aperturas repetidas.
- `LevelSpawner` calcula cada aparicion de DealerFish como `intervaloBase * random(min, max)`. El contrato actual usa `random(1, 3)`.
- `dealerFishSpawnZoneMin` y `dealerFishSpawnZoneMax` estan limitados por codigo a la mitad inferior: `0` equivale a `BottomBoundary`, `0.5` equivale al centro.
- La UI de tienda pertenece a la escena; el manager no autogenera canvas.

## Flujo de interaccion

1. El jugador avanza de forma continua.
2. Se aproxima a una amenaza y `GrazeDetector` carga Ink-Pulse.
3. En `ZonaAbisopelagica`, las entidades con `LightGrazeSource` revelan localmente `LayerBlack` dentro del overlay compuesto.
4. `InkPulseController` pasa de `Idle` a `Charging` o `Ready`.
5. Si el jugador activa el recurso, `InkPulseController` entra en `Active`.
6. `PlayerMovement` ajusta velocidad y comportamiento mientras el pulso esta activo, `PlayerVisualStateController` muestra `InkPulseVisual`, oculta temporalmente `SquidVisual` y `InkPulseMusicCrossfader` cruza hacia la pista intensa.
7. `PlayerCollision` y sistemas de entorno resuelven impactos.
8. Antes de Game Over, `PlayerGadgetInventory` puede consumir `Shell Shield`.
9. Cruzar un portal fuerza `PlayerRuntimeState.PortalTransition`, muestra solo `PortalVisual`, espera `PortalEffect` y luego carga la zona destino.
10. Cruzar un portal conserva gadgets e Ink-Pulse; Game Over los reinicia.

## Reglas de diseno

- El jugador no debe depender de flags sueltas para su estado runtime.
- La carga del Ink-Pulse debe ser legible desde la UI.
- El riesgo debe generar valor real.
- La recoleccion de camarones no debe romper el ritmo del runner.
- Los tags compartidos (`Player`, `Shrimp`, `Collectible`, `Portal`) deben provenir de `GameplayTagCatalog`.
- Los limites verticales del jugador deben provenir de `PlayerBoundaries`.
- Los parametros ajustables de gameplay deben vivir en managers/controladores, no en entidades de colision o prefabs de evento.
- El light graze visual no debe mezclarse con la carga mecanica de Ink-Pulse.
- La animacion visual de Ink-Pulse debe vivir en `InkPulseVisual`, separada de `SquidVisual`, para poder dimensionar el sprite largo sin deformar el cuerpo del jugador ni dibujar dos cuerpos a la vez.
- La animacion visual de portal debe vivir en `PortalVisual`; su prioridad visual es mayor que Ink-Pulse porque representa una transicion de escena.
