# GameplaySystems

## Alcance

Este documento agrupa los sistemas que definen la experiencia directa del jugador: movimiento, recurso Ink-Pulse, graze, colisiones, recolección de camarones, inventario de gadgets y estado runtime del personaje.

## Sistemas principales

### PlayerMovement

Archivo: `Assets/Implementation/Code/Player/Movement/PlayerMovement.cs`

Responsabilidad:
- Mover al jugador en horizontal con avance continuo.
- Ajustar la posición vertical según el input del mouse.
- Aplicar límites verticales usando las fronteras de escena.
- Cambiar velocidad y tilt durante Ink-Pulse.

### InkPulseController

Archivo: `Assets/Implementation/Code/Player/Abilities/InkPulseController.cs`

Responsabilidad:
- Administrar la carga del recurso Ink-Pulse.
- Exponer el estado `InkPulseState`.
- Activar y finalizar el pulso.
- Notificar cambios de carga y de estado a UI o sistemas externos.
- Permitir que `Ink-Bottle` fuerce el estado `Ready` mediante `TryForceReady()`.

### GrazeDetector

Archivo: `Assets/Implementation/Code/Player/Interaction/GrazeDetector.cs`

Responsabilidad:
- Detectar proximidad útil con enemigos u obstáculos.
- Alimentar la carga del Ink-Pulse sin requerir colisión.
- Reconocer amenazas mediante `EnemyTagCatalog`, no mediante tags editables en Inspector.

### PlayerCollision

Archivo: `Assets/Implementation/Code/Player/Interaction/PlayerCollision.cs`

Responsabilidad:
- Resolver colisiones del jugador con amenazas u objetos relevantes.
- Disparar consecuencias de daño, derrota o interacción especial.
- Delegar la identificación de enemigos en `EnemyTagCatalog`.
- Consultar `PlayerGadgetInventory` antes de declarar Game Over, para consumir `Shell Shield` si existe.
- Reconocer camarones mediante `GameplayTagCatalog.Shrimp`.

### ShrimpCollector y ShrimpValue

Archivos:
- `Assets/Implementation/Code/Player/Interaction/ShrimpCollector.cs`
- `Assets/Implementation/Code/Player/Interaction/ShrimpValue.cs`
- `Assets/Implementation/Code/Player/Interaction/ShrimpRuntimeWallet.cs`
- `Assets/Implementation/Code/Core/World/GameplayTagCatalog.cs`

Responsabilidad:
- Registrar la recolección de camarones.
- Definir cuánto vale cada recogible.
- Alimentar una billetera runtime persistente durante la ejecución del juego.
- Mantener el total entre reinicios de escena mientras el proceso siga abierto.
- Centralizar los tags compartidos de gameplay para evitar strings duplicados.

### PlayerGadgetInventory

Archivos:
- `Assets/Implementation/Code/Player/Inventory/GadgetDefinitions.cs`
- `Assets/Implementation/Code/Player/Inventory/RuntimeGadgetInventory.cs`
- `Assets/Implementation/Code/Player/Inventory/PlayerGadgetInventory.cs`
- `Assets/Implementation/Code/Player/Inventory/GadgetPickup.cs`

Responsabilidad:
- Inicializar el inventario runtime de gadgets.
- Recibir adquisiciones desde prefabs con `GadgetPickup`.
- Almacenar gadgets en slots de inventario según el orden de adquisición.
- Modelar posesión única con `HasGadget`, no con contadores ni stacks.
- Mostrar tecla solo cuando el gadget del slot es activo.
- Consumir `Shell Shield` automáticamente cuando una colisión produciría Game Over.
- Activar gadgets de slot con teclado: `W` para slot 1 y `Q` para slot 2.
- Consumir `Ink-Bottle` sólo si pudo llevar el Ink-Pulse a `Ready`.

Regla de slots:
- El prefab no define si un gadget va en `W` o `Q`.
- Al adquirir un gadget, `RuntimeGadgetInventory` lo coloca en el primer slot libre.
- Ningún gadget es stackable: si ya se posee, otro pickup del mismo tipo no se consume ni aumenta cantidad.
- `Shell Shield` es pasivo: ocupa slot visual, no muestra tecla, y se consume automáticamente al evitar un Game Over.

## Flujo de interacción

1. El jugador avanza de forma continua.
2. Se aproxima a una amenaza y el `GrazeDetector` habilita carga.
3. `InkPulseController` sube de `Idle` a `Charging` o `Ready`.
4. Si el jugador activa el recurso, `InkPulseController` entra en `Active`.
5. `PlayerMovement` ajusta velocidad y comportamiento mientras el pulso está activo.
6. `PlayerCollision` y los sistemas de entorno resuelven impactos o derrotas.
7. Antes de Game Over, `PlayerGadgetInventory` puede consumir `Shell Shield` y cancelar la derrota.

## Reglas de diseño

- El jugador no debe depender de flags sueltas para su estado runtime.
- La carga del Ink-Pulse debe ser legible desde la UI.
- El riesgo debe sentirse útil: acercarse al peligro tiene que generar valor real.
- La recolección de camarones no debe romper el ritmo del runner.
- Los tags compartidos (`Player`, `Shrimp`, `Collectible`) deben provenir de `GameplayTagCatalog`.
