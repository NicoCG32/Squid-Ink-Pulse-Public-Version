# Sistemas de gameplay

## Alcance

Este documento agrupa los sistemas que definen la experiencia directa del jugador: movimiento, Ink-Pulse, graze, light graze visual, colisiones, camarones, inventario de gadgets y tienda temporal.

## PlayerMovement

Archivo: `Assets/Implementation/Code/Player/Movement/PlayerMovement.cs`

Prefab canónico: `Assets/Content/Prefabs/Player/BabySquid.prefab`

Responsabilidad:
- Mover al jugador en horizontal con avance continuo.
- Ajustar la posición vertical según el objetivo semántico de entrada.
- Aplicar límites verticales usando `PlayerBoundaries`.
- Cambiar velocidad y tilt durante Ink-Pulse.
- Opcionalmente elegir una Y inicial aleatoria dentro de `PlayerBoundaries`.

`PlayerMovement` consume `Gameplay/SteerPosition` mediante el lector runtime descrito en [Input.md](Input.md). La posición continúa expresada en píxeles de pantalla; el controlador conserva el límite pantalla→mundo mediante `Camera.ScreenToWorldPoint` y aplica la misma velocidad, clamp y actualización de tilt que el control Windows original.

`PlayerVerticalMovementPolicy` mantiene pura la decisión entre objetivo del jugador e impulso externo. Un impulso de Jellyfish activo tiene prioridad durante toda su ventana, incluso contra un objetivo opuesto o mientras el jugador está en el límite superior. El último paso usa sólo el tiempo restante; el objetivo del jugador se retoma en el frame siguiente. Solicitudes repetidas conservan por separado la mayor velocidad y la ventana más larga.

Si el lector todavía no recibió una posición válida, no existe objetivo vertical. `(0,0)` sigue siendo una coordenada válida y no se usa como sentinel. El binding versionado de `SteerPosition` continúa siendo sólo mouse, pero `TouchSteeringSurface` puede inyectar posiciones de pantalla desde un único dedo con prioridad temporal. Al soltar o cancelar vuelve al último mouse válido, o invalida el objetivo si no existe fallback. La superficie vive en la instancia de `TouchControls.prefab` montada bajo el HUD de ambos GameRoot activos.

Contrato de límites:
- No usa `minY` ni `maxY` serializados.
- No recibe `topBorder` ni `bottomBorder` por Inspector.
- Si `PlayerBoundaries/TopBoundary` o `PlayerBoundaries/BottomBoundary` faltan, el problema es de escena.

Contrato de prefab:
- Las escenas jugables usan una instancia llamada `Squid`, pero la fuente editable es `BabySquid.prefab`.
- El prefab base no guarda referencias externas a sesión, cámara, HUD, progression director ni boundaries.
- Las instancias de escena tienen esas referencias externas asignadas en Inspector.
- Los componentes del jugador resuelven esas referencias en runtime solo como respaldo.
- Los cambios de collider, visuales base, `GrazeZone`, inventario o Ink-Pulse deben aplicarse al prefab.

## InkPulseController

Archivo: `Assets/Implementation/Code/Player/Abilities/InkPulseController.cs`

Estado runtime: `Assets/Implementation/Code/Player/Abilities/RuntimeInkPulseState.cs`

Feedback musical:
- `Assets/Implementation/Code/Audio/InkPulseMusicCrossfader.cs`
- `Assets/Implementation/Code/Audio/SoundtrackPitchProgression.cs`

Feedback visual: `Assets/Implementation/Code/Player/Visual/PlayerVisualStateController.cs`

Responsabilidad:
- Administrar la carga del recurso Ink-Pulse.
- Exponer `InkPulseState`.
- Activar y finalizar el pulso.
- Notificar cambios de carga y estado a UI o sistemas externos.
- Permitir que `Ink-Bottle` fuerce el estado `Ready` mediante `TryForceReady()`.
- Persistir carga, estado activo y tiempo restante entre portales.
- Reiniciarse cuando `GameSessionController` entra en `GameSessionState.GameOver`.
- Durante `Active`, la carga mecánica ya queda consumida, pero `ChargeBar` muestra `PulseRemainingSeconds / PulseDuration` para que la InkBar se vacie gradualmente mientras dura el pulso.
- Bloquear activacion manual mientras `InGameShopManager` esta mostrando una oferta temporal.
- Bloquear activacion nueva mientras `PlayerStateController` esta en `PlayerRuntimeState.PortalTransition`.
- Exponer eventos para feedback externo, incluida la mezcla musical del soundtrack normal y `INK`.
- Exponer duracion y tiempo restante para que animaciones puedan ajustarse al estado `Active`.

Input:
- `InkPulseController` ya no consulta dispositivos. `InkPulseInputBinding` recibe `Gameplay/ActivateInkPulse`, conserva una solicitud por frame y el controlador llama a `TryActivatePulse()` al comienzo de su `Update`.
- Los bindings de escritorio siguen siendo click izquierdo y tecla `Space`; ambos producen una solicitud por pulsación y pasan por la misma comprobación de dominio.
- La solicitud no activa durante tienda, portal, muerte, Game Over ni antes de `Ready`.
- El binding se dispone usando la referencia exacta del lector suscrito y se reemplaza mediante `GameplayChanged` si se recrea sólo el scope, por lo que no retiene callbacks entre escenas ni queda unido a un lector obsoleto.

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
- `SoundtrackPitchProgression` vive en el mismo nodo `Soundtrack` cuando la zona usa pitch progresivo.
- En `ZonaEpipelagica`, el pitch progresivo afecta ambas pistas para mantenerlas musicalmente alineadas durante el crossfade.
- En `ZonaAbisopelagica`, afecta la pista unica del soundtrack.
- El pitch usa `RuntimePlayerPace.ElapsedSpeedSeconds`; no debe aumentar en pausa, Game Over ni transiciones donde la run no avanza.

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
- Filtrar fuentes fuera de cámara y recomponer solo a una frecuencia configurable para evitar picos de FPS.
- Mantenerse independiente del `GrazeDetector` y del `GrazeZone`.
- No cargar Ink-Pulse ni modificar economia, dano o colisiones.

Reglas:
- Los parámetros visuales viven en `ZoneLightingController`.
- En modo compuesto, `LayerBlack` usa una textura generada por `ZoneLightingController` y `SpriteRenderer.maskInteraction = None`.
- `SpriteRenderer.maskInteraction = VisibleOutsideMask` solo corresponde al fallback legacy con `SpriteMask`.
- En modo compuesto, el algoritmo debe pintar solo el area de pixeles afectada por cada fuente visible, no comparar cada pixel contra todas las fuentes activas.
- La instancia `Squid` de `BabySquid.prefab` en `ZonaAbisopelagica` declara `LightGrazeSource` como override de escena.
- `SpawnedObjectConfigurator`, invocado por `LevelSpawner`, agrega `LightGrazeSource` a camarones, enemigos, `DealerFish` y portales solo si la zona activa tiene `ZoneLightingController`.

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
- Alimentar una billetera persistente respaldada por `player-records.json`.
- Mostrar el total en HUD.

Estado actual:
- `ShrimpCoin` vale `1`.
- `ShrimpCoinX10` vale `10`.
- El total persiste fuera del juego mediante `PersistentPlayerProfile`.
- La tienda in-run gasta desde la misma billetera persistente.
- Los reembolsos usan `ShrimpRuntimeWallet.Refund` para no inflar `totalShrimpsCollected`.
- La mejora permanente `upgrade.shrimp_multiplier` aumenta la recompensa antes de guardarla en `player-records.json`.

## Score de run

Archivos:
- `Assets/Implementation/Code/Core/Session/RuntimeRunScore.cs`
- `Assets/Implementation/Code/Core/Session/RuntimePlayerPace.cs`
- `Assets/Implementation/Code/Core/Session/RunProgressionDirector.cs`
- `Assets/Implementation/Code/UI/HUD/ScoreCounterDisplay.cs`

Responsabilidad:
- Registrar avance abstracto de la partida como puntaje.
- Subir rápidamente mientras la sesión esta en gameplay activo.
- Persistir entre portales.
- Conservar el valor acumulado al pasar de `ZonaEpipelagica` a `ZonaAbisopelagica`.
- Pausar acumulacion desde el contacto con portal mientras la run esta en `RunEventState.Transitioning`.
- Capturar el puntaje final al entrar en Game Over antes de reiniciar el contador runtime.
- Reiniciarse al pulsar reintentar, porque se inicia una run nueva desde `ZonaEpipelagica`.
- Alimentar sistemas de progresión como precios de tienda.
- Separar score y velocidad del flujo de intensidad de spawns.

Reglas:
- El score no es moneda y no se gasta.
- `RunProgressionDirector` acumula el valor; `ScoreCounterDisplay` solo lo muestra.
- El HUD puede tener un nodo `Score` con `TextMeshProUGUI`; la utilidad de escena le asigna `ScoreCounterDisplay`.
- `RuntimeRunScore.LastCompletedScore` conserva el puntaje final de la ultima run terminada para que `GameOverMenuManager` lo muestre aunque `TotalScore` ya haya vuelto a cero.
- `RuntimePlayerPace` acumula la progresión de velocidad del calamar y persiste entre portales.
- La velocidad horizontal normal crece de forma asintotica desde `minScrollSpeed` hacia `maxScrollSpeed`.
- La intensidad de spawn usa otra curva: baja a alta, boss, post-boss intenso; cruzar portal reinicia esa intensidad en la zona destino.
- La mejora permanente `upgrade.score_multiplier` multiplica el score producido por `RunProgressionDirector`.

Valores globales vigentes en `GameRoot_ZonaEpipelagica` y `GameRoot_ZonaAbisopelagica`:
- `secondsToMaxIntensity = 150`
- `maxScrollSpeed = 15`
- `speedGrowthTimeConstantSeconds = 120`
- `scorePerSecond = 3200`

## Gadgets e inventario

Archivos:
- `Assets/Implementation/Code/Player/Inventory/GadgetId.cs`
- `Assets/Implementation/Code/Player/Inventory/GadgetActivationKind.cs`
- `Assets/Implementation/Code/Player/Inventory/GadgetCatalog.cs`
- `Assets/Implementation/Code/Player/Inventory/RuntimeGadgetInventory.cs`
- `Assets/Implementation/Code/Player/Inventory/PlayerGadgetInventory.cs`
- `Assets/Implementation/Code/Player/Inventory/GadgetShopItem.cs`
- `Assets/Implementation/Code/UI/HUD/GadgetInventoryHud.cs`
- `Assets/Implementation/Code/Player/Profile/RunGadgetUnlockService.cs`

Responsabilidad:
- Inicializar inventario runtime.
- Registrar prefabs de gadget como mercancia comprable mediante `GadgetShopItem`.
- Almacenar gadgets en slots segun orden de adquisicion.
- Modelar posesion unica con `HasGadget`, no con contadores ni stacks.
- Mostrar tecla solo cuando el gadget del slot es activo.
- Activar `Gadget1` con `Q` y `Gadget2` con `W` mediante `Gameplay/UseGadgetSlot1` y `Gameplay/UseGadgetSlot2`.
- Persistir entre portales mediante `RuntimeGadgetInventory`.
- Reiniciarse cuando `GameSessionController` entra en `GameSessionState.GameOver`.
- Separar compras de run de desbloqueos permanentes: el inventario guarda lo comprado en la run; `RunGadgetUnlockService` solo decide si ese gadget puede aparecer en la tienda temporal.

Gadgets implementados:
- `Shell Shield`: pasivo, se consume automaticamente para cancelar un Game Over.
- `Ink-Bottle`: activo de run, fuerza Ink-Pulse a `Ready` si puede y se consume solo cuando el efecto se ejecuta correctamente.

Reglas de slot:
- El prefab no define si un gadget va en `Q` o `W`.
- El primer gadget activo en `Gadget1` usa `Q`.
- El segundo gadget activo en `Gadget2` usa `W`.
- Los pasivos ocupan slot visual, pero no muestran tecla.
- Ningun gadget es stackable.
- Todo gadget comprado durante la run es de un solo uso: al consumirse libera su slot y desaparece del HUD.
- `TryUseSlot1()` y `TryUseSlot2()` son las operaciones públicas compartidas con los botones touch. Ambas validan sesión activa, bloqueo de tienda, gadget activo y poseído, éxito del efecto y consumo; no se permite que la UI llame directamente a `RuntimeGadgetInventory.TryConsume()`.

## Tienda temporal de suministros

Archivos:
- `Assets/Implementation/Code/UI/Shop/InGameShopManager.cs`
- `Assets/Implementation/Code/World/Shop/DealerFish.cs`
- `Assets/Implementation/Code/Spawning/LevelSpawner.cs`

Responsabilidad:
- Instanciar `DealerFish` desde `LevelSpawner`.
- Ubicar `DealerFish` dentro de una zona normalizada configurable de la mitad inferior del rango entre `PlayerBoundaries`.
- Separar intervalo base de aparición y variacion aleatoria de cadencia.
- Abrir un overlay temporal al colisionar con `DealerFish`.
- Mostrar el comic `ShopInGameFirst` antes de la primera apertura por `DealerFish`.
- Seleccionar un gadget aleatorio desde ofertas configuradas.
- Filtrar ofertas mediante `RunGadgetUnlockService`, de modo que solo aparezcan gadgets habilitados por defecto o por hitos de perfil.
- Mostrar icono, precio, tecla `B`, boton `Comprar`, contador y mensaje de saldo.
- Consumir camarones solo si la compra se concreta.
- Registrar el gadget comprado en `RuntimeGadgetInventory`.
- Al cerrar la primera tienda abierta por `DealerFish`, mostrar el comic de salida con compra o sin compra.

Reglas:
- La tienda tiene duracion ajustable por `offerDurationSeconds`.
- Por defecto congela gameplay mientras el contador avanza en tiempo real.
- La compra se intenta con `B` o click sobre el boton `Comprar`.
- `B` es un atajo de producto exclusivo de escritorio expresado por `Gameplay/BuyShopOffer`, con binding sólo Keyboard&Mouse y ninguno Touch. `InGameShopManager` consume esa solicitud; la vía móvil de UI es el botón `Comprar` y ambas llaman al mismo `BuyCurrentOffer()`.
- `SinSaldo` aparece solo despues de intentar comprar sin camarones suficientes.
- El precio se calcula desde score: `((score / 100000) + 1) * aleatorio(1, 2) * precioBaseMinimo`, con parámetros equivalentes en `InGameShopManager`.
- Si el gadget ya existe en inventario, no se compra de nuevo.
- Si el contador llega a cero, la oferta se cierra.
- La ruta de apertura queda clasificada por `InGameShopOpenSource`: `DealerFish` habilita comics de entrada/salida, `Tutorial` fuerza oferta sin activar lore de DealerFish y `Timed` representa apertura temporal normal.
- `DealerFish` se consume solo si `InGameShopManager` acepta la apertura. Tras abrir tienda, conserva su collider trigger para que `DestroyOffscreen` pueda limpiarlo. Las aperturas repetidas se evitan con un flag interno del propio `DealerFish`.
- `RuntimeInGameShopLoreState` limita los intentos de comic de entrada/salida a una vez por run, y `player-profile.json/lore.viewedComicEventIds` impide repetir comics de tienda ya vistos en partidas futuras.
- Al comprar correctamente, la tienda entrega el gadget y se cierra. El feedback visual permanente del vendedor no pertenece a esta tienda, sino a `ShopMenu`.
- Mientras la tienda o su comic previo estan activos, `InkPulseController` y `PlayerGadgetInventory` bloquean activacion de Ink-Pulse e InkBottle.
- `LevelSpawner` calcula cada aparición de DealerFish como `intervaloBase * random(min, max)`. El contrato actual usa `random(1, 3)`.
- `dealerFishSpawnZoneMin` y `dealerFishSpawnZoneMax` estan limitados por código a la mitad inferior: `0` equivale a `BottomBoundary`, `0.5` equivale al centro.
- `ZonaAbisopelagica` referencia `DealerFish_ZonaAbisopelagica.prefab`, que conserva la misma lógica pero oscurece `Visual` y `VisualSupport` a RGB `135,135,135`.
- La UI de tienda pertenece a la escena; el manager no autogenera canvas.

## Flujo de interaccion

1. El jugador avanza de forma continua.
2. Se aproxima a una amenaza y `GrazeDetector` carga Ink-Pulse.
3. En `ZonaAbisopelagica`, las entidades con `LightGrazeSource` revelan localmente `LayerBlack` dentro del overlay compuesto.
4. `InkPulseController` pasa de `Idle` a `Charging` o `Ready`.
5. Si el jugador activa el recurso, `InkPulseController` entra en `Active`.
6. Mientras el pulso esta activo, `InkBar` consume visualmente su llenado de forma progresiva hasta llegar a cero.
7. `PlayerMovement` ajusta velocidad y comportamiento mientras el pulso esta activo, `PlayerVisualStateController` muestra `InkPulseVisual`, oculta temporalmente `SquidVisual` y `InkPulseMusicCrossfader` cruza hacia la pista intensa.
8. `PlayerCollision` y sistemas de entorno resuelven impactos.
9. Antes de Game Over, `PlayerGadgetInventory` puede consumir `Shell Shield`.
10. Cruzar un portal fuerza `PlayerRuntimeState.PortalTransition`, muestra solo `PortalVisual`, espera `PortalEffect` y luego carga la zona destino.
11. Cruzar un portal conserva gadgets e Ink-Pulse; Game Over los reinicia.

## Reglas de diseño

- El jugador no debe depender de flags sueltas para su estado runtime.
- La carga del Ink-Pulse debe ser legible desde la UI.
- El riesgo debe generar valor real.
- La recoleccion de camarones no debe romper el ritmo del runner.
- Los tags compartidos (`Player`, `Shrimp`, `Collectible`, `Portal`) deben provenir de `GameplayTagCatalog`.
- Los límites verticales del jugador deben provenir de `PlayerBoundaries`.
- Los parámetros ajustables de gameplay deben vivir en managers/controladores, no en entidades de colision o prefabs de evento.
- El light graze visual no debe mezclarse con la carga mecánica de Ink-Pulse.
- La animación visual de Ink-Pulse debe vivir en `InkPulseVisual`, separada de `SquidVisual`, para poder dimensionar el sprite largo sin deformar el cuerpo del jugador ni dibujar dos cuerpos a la vez.
- La animación visual de portal debe vivir en `PortalVisual`; su prioridad visual es mayor que Ink-Pulse porque representa una transición de escena.
