# Hoja de ruta

## Alcance

Este documento agrupa las piezas que todavía no están implementadas pero ya tienen dirección funcional en el diseño del proyecto.

## SupplyEventState

Nombre propuesto: `SupplyEventState`

Controlador propuesto: `SupplyEventDirector`

Idea general:
- Tienda in-run sin pausar la partida.
- Apariciones controladas por cooldown, probabilidad y ventanas cortas de interacción.
- No debe romper el ritmo de persecución.

## PortalTransitionState

Nombre propuesto: `PortalTransitionState`

Controlador propuesto: `PortalTransitionController`

Idea general:
- Transición entre zonas o escenas después de un boss o una ventana de oportunidad.
- Debe nacer desde `RunEventState.PostBossWindow`.
- Debe pedir `RunEventState.Transitioning` antes de ejecutar el cambio.

## GadgetRuntimeState

Nombre propuesto: `GadgetRuntimeState`

Controlador propuesto: `GadgetInventoryController` o controladores por gadget.

Idea general:
- Inventario de gadgets con estados como `Locked`, `Available`, `Stored`, `Active`, `Consumed` y `Cooldown`.
- Debe integrarse con `InkPulseState` y con la sesión global.

## Cómo se conecta con el resto del proyecto

- El roadmap no reemplaza a `StateMachines.md`; lo complementa con intención de diseño.
- Cuando una idea se vuelva implementación real, se mueve a su módulo correspondiente.
- La meta es que ninguna mecánica futura nazca como un `bool` suelto si necesita reglas claras de entrada y salida.
