# GameplaySystems

## Alcance

Este documento agrupa los sistemas que definen la experiencia directa del jugador: movimiento, recurso Ink-Pulse, graze, colisiones, recolección de camarones y estado runtime del personaje.

## SistemasPrincipales

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

### ShrimpCollector y ShrimpValue

Archivos:
- `Assets/Implementation/Code/Player/Interaction/ShrimpCollector.cs`
- `Assets/Implementation/Code/Player/Interaction/ShrimpValue.cs`

Responsabilidad:
- Registrar la recolección de camarones.
- Definir cuánto vale cada recogible.
- Alimentar la economía del juego y la progresión de run.

## FlujoDeInteraccion

1. El jugador avanza de forma continua.
2. Se aproxima a una amenaza y el `GrazeDetector` habilita carga.
3. `InkPulseController` sube de `Idle` a `Charging` o `Ready`.
4. Si el jugador activa el recurso, `InkPulseController` entra en `Active`.
5. `PlayerMovement` ajusta velocidad y comportamiento mientras el pulso está activo.
6. `PlayerCollision` y los sistemas de entorno resuelven impactos o derrotas.

## ReglasDeDiseno

- El jugador no debe depender de flags sueltas para su estado runtime.
- La carga del Ink-Pulse debe ser legible desde la UI.
- El riesgo debe sentirse útil: acercarse al peligro tiene que generar valor real.
- La recolección de camarones no debe romper el ritmo del runner.
