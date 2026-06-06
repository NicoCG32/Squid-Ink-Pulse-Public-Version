# Sistemas de gameplay

## Alcance

Este documento agrupa los sistemas que definen la experiencia directa del jugador: movimiento, Ink-Pulse, graze, light graze visual, colisiones, camarones, inventario de gadgets y tienda temporal.

## PlayerMovement

Archivo: `Assets/Implementation/Code/Player/Movement/PlayerMovement.cs`

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

## InkPulseController

Archivo: `Assets/Implementation/Code/Player/Abilities/InkPulseController.cs`

Estado runtime: `Assets/Implementation/Code/Player/Abilities/RuntimeInkPulseState.cs`

Responsabilidad:
- Administrar la carga del recurso Ink-Pulse.
- Exponer `InkPulseState`.
- Activar y finalizar el pulso.
- Notificar cambios de carga y estado a UI o sistemas externos.
- Permitir que `Ink-Bottle` fuerce el estado `Ready` mediante `TryForceReady()`.
- Persistir carga, estado activo y tiempo restante entre portales.
- Reiniciarse cuando `GameSessionController` entra en `GameSessionState.GameOver`.
- Bloquear activacion manual mientras `InGameShopManager` esta mostrando una oferta temporal.

Estados:
- `Idle`
- `Charging`
- `Ready`
- `Active`

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
- `Assets/Implementation/Code/World/Lighting/LightGrazeProbe.cs`

Responsabilidad:
- Oscurecer `ZonaExe` mediante un overlay de escena.
- Revelar temporalmente el fondo cuando el BabySquid pasa cerca de entidades con `LightGrazeSource`.
- Mantenerse independiente del `GrazeDetector` y del `GrazeZone`.
- No cargar Ink-Pulse ni modificar economia, dano o colisiones.

Reglas:
- Los parametros visuales viven en `ZoneLightingController`.
- Las entidades solo declaran `LightGrazeSource`.
- El BabySquid declara `LightGrazeProbe`.
- La sonda mide contra colliders habilitados o bounds visuales, no solo contra el pivote del objeto.

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
- `Assets/Implementation/Code/UI/HUD/ShrimpCounterDisplay.cs`

Responsabilidad:
- Registrar recoleccion de camarones.
- Definir el valor de cada recogible.
- Alimentar una billetera runtime persistente durante la ejecucion del juego.
- Mostrar el total en HUD.

Estado actual:
- `ShrimpCoin` vale `1`.
- `ShrimpCoinX10` vale `10`.
- El total persiste durante runtime.
- La persistencia fuera de runtime sigue pendiente.

## Gadgets e inventario

Archivos:
- `Assets/Implementation/Code/Player/Inventory/GadgetDefinitions.cs`
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
- Ubicar `DealerFish` en el cuarto inferior del rango entre `PlayerBoundaries`.
- Abrir un overlay temporal al colisionar con `DealerFish`.
- Seleccionar un gadget aleatorio desde ofertas configuradas.
- Mostrar icono, precio, tecla `B`, contador y mensaje de saldo.
- Consumir camarones solo si la compra se concreta.
- Registrar el gadget comprado en `RuntimeGadgetInventory`.

Reglas:
- La tienda tiene duracion ajustable por `offerDurationSeconds`.
- Por defecto congela gameplay mientras el contador avanza en tiempo real.
- La compra se intenta con `B`.
- `SinSaldo` aparece solo despues de intentar comprar sin camarones suficientes.
- Si el gadget ya existe en inventario, no se compra de nuevo.
- Si el contador llega a cero, la oferta se cierra.
- La UI de tienda pertenece a la escena; el manager no autogenera canvas.

## Flujo de interaccion

1. El jugador avanza de forma continua.
2. Se aproxima a una amenaza y `GrazeDetector` carga Ink-Pulse.
3. En `ZonaExe`, `LightGrazeProbe` puede revelar el fondo si esa entidad tiene `LightGrazeSource`.
4. `InkPulseController` pasa de `Idle` a `Charging` o `Ready`.
5. Si el jugador activa el recurso, `InkPulseController` entra en `Active`.
6. `PlayerMovement` ajusta velocidad y comportamiento mientras el pulso esta activo.
7. `PlayerCollision` y sistemas de entorno resuelven impactos.
8. Antes de Game Over, `PlayerGadgetInventory` puede consumir `Shell Shield`.
9. Cruzar un portal conserva gadgets e Ink-Pulse; Game Over los reinicia.

## Reglas de diseno

- El jugador no debe depender de flags sueltas para su estado runtime.
- La carga del Ink-Pulse debe ser legible desde la UI.
- El riesgo debe generar valor real.
- La recoleccion de camarones no debe romper el ritmo del runner.
- Los tags compartidos (`Player`, `Shrimp`, `Collectible`, `Portal`) deben provenir de `GameplayTagCatalog`.
- Los limites verticales del jugador deben provenir de `PlayerBoundaries`.
- Los parametros ajustables de gameplay deben vivir en managers/controladores, no en entidades de colision o prefabs de evento.
- El light graze visual no debe mezclarse con la carga mecanica de Ink-Pulse.
