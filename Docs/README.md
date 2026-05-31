# Índice de documentación

Este directorio organiza la documentación por tema. La idea es que cada persona encuentre rápido lo que necesita sin leer un solo archivo gigante.

## Base del proyecto

- [ProjectOverview.md](ProjectOverview.md) - visión general, pilares y orden de lectura.
- [ProjectStructure.md](ProjectStructure.md) - organización de carpetas y criterios.
- [RuntimeHierarchyAudit.md](RuntimeHierarchyAudit.md) - asignación esperada de scripts por nodo, prefab y responsabilidad.
- [ZonaEpipelagica.md](ZonaEpipelagica.md) - estructura de la escena principal, managers y jerarquía runtime.
- [AssetFlow.md](AssetFlow.md) - entrada y uso de audio, arte y animaciones.
- [WorldScale.md](WorldScale.md) - escala canónica entre píxeles, unidades de mundo y boundaries.
- [AnimationStandards.md](AnimationStandards.md) - reglas y checklist de animaciones runtime.

## Sistemas de juego

- [GameplaySystems.md](GameplaySystems.md) - movimiento, Ink-Pulse, graze, colisiones y camarones.
- [CoreSystems.md](CoreSystems.md) - sesión, progresión de run y flujo de escenas.
- [EnemiesAndBosses.md](EnemiesAndBosses.md) - spawn, enemigos base y SS Carnage.
- [WorldAndCamera.md](WorldAndCamera.md) - cámara, fronteras, parallax y destrucción fuera de pantalla.
- [UiAndMenu.md](UiAndMenu.md) - menús, pausa, game over y HUD.

## Diseño y futuro

- [StateMachines.md](StateMachines.md) - estados implementados y planificados.
- [ROADMAP.md](ROADMAP.md) - ideas y sistemas todavía no implementados.
- [ProjectReport.md](ProjectReport.md) - resumen del informe completo del proyecto.
- [Reports/InformeSquidInkPulse.md](Reports/InformeSquidInkPulse.md) - informe largo en CamelCase.

## Regla general

- Si algo cambia el comportamiento del juego, debería vivir en un documento propio.
- Si algo describe un flujo de producción o contenido, debe quedar separado de la lógica de código.
- Si una sección empieza a crecer demasiado, se divide en otro `.md`.
